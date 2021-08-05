using Microsoft.AspNetCore.Http;
using PuppyPdfGenerator.Database;
using PuppyPdfGenerator.Database.Tables;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace PuppyPdfGenerator.PdfGenerator
{
    public class Engine
    {
        /// <summary>
        /// Create and execute the job.
        /// </summary>
        public static async Task<Job> Create(
            HttpRequest request,
            Job.Instructions instructions,
            CancellationToken cancellationToken)
        {
            await using var db = new DatabaseContext();

            var start = DateTimeOffset.Now;

            var job = new Job
            {
                Started = start,
                RequestIp = request.HttpContext.Connection.RemoteIpAddress?.ToString(),
                InstructionsSerialized =
                    JsonSerializer.Serialize(
                        instructions,
                        new JsonSerializerOptions
                        {
                            IgnoreNullValues = true,
                            PropertyNameCaseInsensitive = true,
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                        })
            };

            await db.Jobs.AddAsync(job, cancellationToken);
            await db.SaveChangesAsync(cancellationToken);

            try
            {
                await job.ExecuteAsync(cancellationToken);

                if (job.JobFinishedBytes == null ||
                    job.JobFinishedBytes.Length == 0)
                {
                    throw new Exception("Final PDF is empty!");
                }

                job.Finished = DateTimeOffset.Now;
            }
            catch (Exception ex)
            {
                while (true)
                {
                    if (ex == null)
                    {
                        break;
                    }

                    var error = new JobError
                    {
                        Created = DateTimeOffset.Now,
                        JobId = job.Id,
                        ErrorMessage = ex.Message,
                        StackTrace = ex.StackTrace
                    };

                    await db.JobErrors.AddAsync(error, cancellationToken);
                    await db.SaveChangesAsync(cancellationToken);

                    job.Errors ??= new List<JobError>();
                    job.Errors.Add(error);

                    ex = ex.InnerException;
                }
            }

            var end = DateTimeOffset.Now;
            var duration = end - start;

            if (job.Finished == null)
            {
                job.Failed = end;
            }

            job.TotalTimeMs = (int) duration.TotalMilliseconds;

            await db.SaveChangesAsync(cancellationToken);

            return job;
        }
    }
}