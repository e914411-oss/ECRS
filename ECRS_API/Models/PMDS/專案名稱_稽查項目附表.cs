using System;
using System.Collections.Generic;

namespace ECRS_API.Models.PMDS
{
    public class 專案名稱_稽查項目附表
    {
        public int 主鍵
        {
            get; set;
        }
        public int 專案名稱代碼主鍵
        {
            get; set;
        }
        public string 稽查項目
        {
            get; set;
        }
        public string 稽查項目代碼
        {
            get; set;
        }
    }
}
