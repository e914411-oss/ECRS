using System;
using System.Collections.Generic;

namespace ECRS_WEB.Models.ECRS;

public partial class vw_Finished_Modify_ckYearList
{
    public long? ROWID { get; set; }

    public int? Industry_ID { get; set; }

    public DateTime? CHKDATE { get; set; }

    public DateTime? MODDATE { get; set; }

    public string? ModReason { get; set; }

    public string? ModUser { get; set; }

    public string? Alter_Field { get; set; }

    public string? TYPE { get; set; }

    public string? GOODNAME { get; set; }
}
