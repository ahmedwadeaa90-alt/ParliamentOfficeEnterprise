using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParliamentOffice.UI.Data.Models
{
    internal class AchievementAttachment
    {
    }
}
namespace ParliamentOffice.UI.Data.Models
{
    public class AchievementAttachment
    {
        public int Id { get; set; }

        public int AchievementId { get; set; }

        public string FileName { get; set; } = string.Empty;

        public string FilePath { get; set; } = string.Empty;

        public string FileType { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public string Notes { get; set; } = string.Empty;
    }
}