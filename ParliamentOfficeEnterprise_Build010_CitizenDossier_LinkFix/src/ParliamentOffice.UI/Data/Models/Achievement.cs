using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System;

namespace ParliamentOffice.UI.Data.Models
{
    public class Achievement
    {
        public int Id { get; set; }

        public string ActivityNumber { get; set; } = string.Empty;

        public DateTime ActivityDate { get; set; } = DateTime.Now;

        public string Title { get; set; } = string.Empty;

        public string Category { get; set; } = string.Empty;

        public string Governorate { get; set; } = string.Empty;

        public string District { get; set; } = string.Empty;

        public string Organization { get; set; } = string.Empty;

        public string Summary { get; set; } = string.Empty;

        public string Keywords { get; set; } = string.Empty;

        public string WordFilePath { get; set; } = string.Empty;

        public string HtmlContent { get; set; } = string.Empty;

        public string AttachmentsFolder { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string CreatedBy { get; set; } = string.Empty;
    }
}