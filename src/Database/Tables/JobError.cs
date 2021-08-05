using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PuppyPdfGenerator.Database.Tables
{
    [Table("JobErrors")]
    public class JobError
    {
        #region ORM

        [Key]
        [Column]
        public long Id { get; set; }

        [Column]
        public long JobId { get; set; }

        [Column]
        public DateTimeOffset Created { get; set; }

        [Column]
        public string ErrorMessage { get; set; }

        [Column]
        public string StackTrace { get; set; }

        #endregion
    }
}