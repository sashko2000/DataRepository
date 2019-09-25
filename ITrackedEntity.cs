using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Data
{
    public interface ITrackedEntity
    {

        [Timestamp]
        byte[] RowVersion { get; set; }

        int? LastEditorId { get; set; }
        //[ForeignKey("LastEditorId")]
        //public User LastEditor { get; set; }

        DateTime? CreateDate { get; set; }
        DateTime? EditDate { get; set; }



    }
}