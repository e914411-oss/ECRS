using System;
using System.Collections.Generic;

namespace ECRS_WEB.Models.ECRS;

public partial class dw_Industry_RegNum_Map
{
    public int ID { get; set; }

    public int? Industry_ID { get; set; }

    public string? Industry_RegNum { get; set; }
}
