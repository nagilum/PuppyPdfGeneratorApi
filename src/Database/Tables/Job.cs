using Microsoft.Playwright;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PuppyPdfGenerator.Database.Tables
{
    [Table("Jobs")]
    public class Job
    {
        #region ORM

        [Key]
        [Column]
        public long Id { get; set; }

        [Column]
        public DateTimeOffset? Started { get; set; }

        [Column]
        public DateTimeOffset? Finished { get; set; }

        [Column]
        public DateTimeOffset? Failed { get; set; }

        [Column]
        public int? TotalTimeMs { get; set; }

        [Column]
        public string RequestIp { get; set; }

        [Column]
        public string InstructionsSerialized { get; set; }

        #endregion

        #region Instance properties

        [NotMapped]
        public List<JobError> Errors { get; set; }

        [NotMapped]
        public byte[] JobFinishedBytes { get; set; }

        #endregion

        #region Instance functions

        /// <summary>
        /// Get the job instructions.
        /// </summary>
        public Instructions GetInstructions()
        {
            return JsonSerializer.Deserialize<Instructions>(
                this.InstructionsSerialized,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    IgnoreNullValues = true
                });
        }

        /// <summary>
        /// Execute the actual job and create the PDF.
        /// </summary>
        public async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            var instructions = this.GetInstructions();

            // Prepare Playwright.
            using var playwright = await Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(
                new BrowserTypeLaunchOptions
                {
                    Headless = true
                });

            // Render entries.
            foreach (var entry in instructions.Entries)
            {
                // Create new page.
                entry.Page = await browser.NewPageAsync();

                // Render given HTML.
                if (entry.Html != null)
                {
                    await entry.Page.SetContentAsync(entry.Html);
                }

                // Render URL.
                if (entry.Url != null)
                {
                    await entry.Page.GotoAsync(entry.Url);
                }

                // Wait for selectors?
                if (entry.WaitForSelectors != null)
                {
                    foreach (var selector in entry.WaitForSelectors)
                    {
                        await entry.Page.WaitForSelectorAsync(selector);
                    }
                }

                // Wait for functions?
                if (entry.WaitForFunctions != null)
                {
                    foreach (var function in entry.WaitForFunctions)
                    {
                        await entry.Page.WaitForFunctionAsync(function);
                    }
                }

                // Render to byte-array.
                entry.PdfBytes = await entry.Page.PdfAsync();
            }

            // Merge PDFs.
            if (instructions.Entries.Count > 1)
            {
                using var pdfDoc = new PdfDocument();

                foreach (var entry in instructions.Entries)
                {
                    var ms = new MemoryStream(entry.PdfBytes);
                    var doc = PdfReader.Open(ms, PdfDocumentOpenMode.Import);

                    for (var i = 0; i < doc.PageCount; i++)
                    {
                        pdfDoc.AddPage(doc.Pages[i]);
                    }
                }

                await using var output = new MemoryStream();

                pdfDoc.Save(output, true);
                this.JobFinishedBytes = output.ToArray();
            }
            else
            {
                this.JobFinishedBytes = instructions.Entries[0].PdfBytes;
            }
        }

        #endregion

        #region Helper classes

        public class Instructions
        {
            /// <summary>
            /// Entries to be rendered.
            /// </summary>
            public List<RenderEntry> Entries { get; set; }

            /// <summary>
            /// Webhooks to call for various job statuses.
            /// </summary>
            public WebHooksWrapper WebHooks { get; set; }

            /// <summary>
            /// Email to send for various job statuses.
            /// </summary>
            public EmailHookWrapper EmailHooks { get; set; }

            public class RenderEntry
            {
                /// <summary>
                /// HTML to render.
                /// </summary>
                public string Html { get; set; }

                /// <summary>
                /// URL to render.
                /// </summary>
                public string Url { get; set; }

                /// <summary>
                /// Wait for CSS selectors.
                /// </summary>
                public string[] WaitForSelectors { get; set; }

                /// <summary>
                /// Wait for JavaScript functions.
                /// </summary>
                public string[] WaitForFunctions { get; set; }

                /// <summary>
                /// Playwright page.
                /// </summary>
                public IPage Page { get; set; }

                /// <summary>
                /// Rendered PDF, as bytes.
                /// </summary>
                public byte[] PdfBytes { get; set; }
            }

            public class WebHooksWrapper
            {

                /// <summary>
                /// Hooks for when the job is started.
                /// </summary>
                public List<WebHookEntry> Started { get; set; }

                /// <summary>
                /// Hooks for when the job is finished.
                /// </summary>
                public List<WebHookEntry> Finished { get; set; }

                /// <summary>
                /// Hooks for if the job fails.
                /// </summary>
                public List<WebHookEntry> Failed { get; set; }

                public class WebHookEntry
                {
                    /// <summary>
                    /// URL to call.
                    /// </summary>
                    public string Url { get; set; }

                    /// <summary>
                    /// HTTP method to use.
                    /// </summary>
                    public string HttpMethod { get; set; } // Defaults to POST

                    /// <summary>
                    /// Headers to include.
                    /// </summary>
                    public Dictionary<string, string> Headers { get; set; }

                    /// <summary>
                    /// Body to send.
                    /// </summary>
                    public string Body { get; set; }

                    /// <summary>
                    /// Whether to parse the body and replace some values.
                    /// </summary>
                    public bool? ParseBody { get; set; }
                }
            }

            public class EmailHookWrapper
            {

                /// <summary>
                /// Hooks for when the job is started.
                /// </summary>
                public List<EmailHookEntry> Started { get; set; }

                /// <summary>
                /// Hooks for when the job is finished.
                /// </summary>
                public List<EmailHookEntry> Finished { get; set; }

                /// <summary>
                /// Hooks for if the job fails.
                /// </summary>
                public List<EmailHookEntry> Failed { get; set; }

                public class EmailHookEntry
                {
                    /// <summary>
                    /// Normal recipients.
                    /// </summary>
                    public string[] ToRecipients { get; set; }

                    /// <summary>
                    /// CC recipients.
                    /// </summary>
                    public string[] CcRecipients { get; set; }

                    /// <summary>
                    /// BCC recipients.
                    /// </summary>
                    public string[] BccRecipients { get; set; }

                    /// <summary>
                    /// E-mail subject.
                    /// </summary>
                    public string Subject { get; set; }

                    /// <summary>
                    /// Whether to parse the subject and replace some values.
                    /// </summary>
                    public bool? ParseSubject { get; set; }

                    /// <summary>
                    /// E-mail body.
                    /// </summary>
                    public string Body { get; set; }

                    /// <summary>
                    /// Whether to parse the body and replace some values.
                    /// </summary>
                    public bool? ParseBody { get; set; }

                    /// <summary>
                    /// Define body as HTML.
                    /// </summary>
                    public bool? BodyIsHtml { get; set; }

                    /// <summary>
                    /// Attach PDF to e-mail.
                    /// </summary>
                    public bool? PdfAsAttachment { get; set; }

                    /// <summary>
                    /// Set filename for PDF attachment.
                    /// </summary>
                    public string AttachmentFilename { get; set; }
                }
            }
        }

        #endregion
    }
}