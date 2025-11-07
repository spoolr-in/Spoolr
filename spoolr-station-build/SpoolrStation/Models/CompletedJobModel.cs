
using System;

namespace SpoolrStation.Models
{
    /// <summary>
    /// Lightweight model for storing metadata of completed jobs
    /// </summary>
    public class CompletedJobModel
    {
        public long JobId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public DateTime CompletedAt { get; set; }
        public decimal FinalPrice { get; set; }
        public string TrackingCode { get; set; } = string.Empty;

        // Display properties
        public string FormattedPrice => $"${FinalPrice:F2}";
        public string CompletionDate => CompletedAt.ToString("yyyy-MM-dd");
        public string CompletionTime => CompletedAt.ToString("HH:mm:ss");
    }
}
