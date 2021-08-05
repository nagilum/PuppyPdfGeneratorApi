using Microsoft.AspNetCore.Mvc;
using PuppyPdfGenerator.Database.Tables;
using PuppyPdfGenerator.PdfGenerator;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PuppyPdfGenerator.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RenderController : ControllerBase
    {
        /// <summary>
        /// Render the given URL or URLs and return the PDF.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> RenderAndDownload(
            [FromQuery] string url,
            [FromQuery] string urls,
            CancellationToken cancellationToken)
        {
            if (url == null &&
                urls == null)
            {
                return this.BadRequest(new
                {
                    message = "The query param 'url' or 'urls' are required."
                });
            }

            var instructions = new Job.Instructions
            {
                Entries = new List<Job.Instructions.RenderEntry>()
            };

            // Add single URL.
            if (url != null)
            {
                instructions.Entries.Add(
                    new Job.Instructions.RenderEntry
                    {
                        Url = url
                    });
            }

            // Add the serialized list of URLs.
            if (urls != null)
            {
                try
                {
                    var list = JsonSerializer.Deserialize<List<string>>(urls);

                    if (list == null)
                    {
                        throw new Exception("Deserialized list of URLs is empty.");
                    }

                    foreach (var singleUrl in list)
                    {
                        instructions.Entries.Add(
                            new Job.Instructions.RenderEntry
                            {
                                Url = singleUrl
                            });
                    }
                }
                catch (Exception ex)
                {
                    return this.BadRequest(new
                    {
                        message = ex.Message
                    });
                }
            }

            var job = await Engine.Create(
                this.Request,
                instructions,
                cancellationToken);

            // If failed, return the job.
            if (job.Failed.HasValue)
            {
                return this.BadRequest(job);
            }

            // Return PDF.
            return this.File(
                new MemoryStream(job.JobFinishedBytes),
                "application/pdf");
        }

        /// <summary>
        /// Render a PDF using the given instructions in the POST payload.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> PerformJob(
            Job.Instructions instructions,
            CancellationToken cancellationToken)
        {
            var job = await Engine.Create(
                this.Request,
                instructions,
                cancellationToken);

            // If failed, return the job.
            if (job.Failed.HasValue)
            {
                return this.BadRequest(job);
            }

            // Return PDF.
            return this.File(
                new MemoryStream(job.JobFinishedBytes),
                "application/pdf");
        }
    }
}