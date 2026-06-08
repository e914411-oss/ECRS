using System;
using System.Collections.Generic;

namespace ECRS_WEB.Models.PMDS
{
    public class 專案名稱_稽查項目代碼表
    {
        public int 稽查項目代碼
        {
            get; set;
        }
        public string 稽查項目
        {
            get; set;
        } = string.Empty;
        public string 專案名稱代碼表_稽查欄位名稱
        {
            get; set;
        } = string.Empty;
        public DateTime 資料有效日期
        {
            get; set;
        }
    }
}
