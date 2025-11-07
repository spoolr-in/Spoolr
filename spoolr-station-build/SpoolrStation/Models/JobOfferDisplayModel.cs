using System;

namespace SpoolrStation.Models
{
    /// <summary>
    /// Display model for showing job offers in the main window
    /// </summary>
    public class JobOfferDisplayModel
    {
        /// <summary>
        /// Unique job identifier
        /// </summary>
        public long JobId { get; set; }

        /// <summary>
        /// Customer name or identifier
        /// </summary>
        public string CustomerName { get; set; } = string.Empty;

        /// <summary>
        /// Name of the file to be printed
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Total price customer pays
        /// </summary>
        public decimal TotalPrice { get; set; }

        /// <summary>
        /// Vendor earnings from this job
        /// </summary>
        public decimal Earnings { get; set; }

        /// <summary>
        /// When the job offer was received
        /// </summary>
        public DateTime ReceivedAt { get; set; }

        /// <summary>
        /// When the job offer expires
        /// </summary>
        public DateTime ExpiresAt { get; set; }

        /// <summary>
        /// Current status of the job offer
        /// </summary>
        public string Status { get; set; } = "Active";

        /// <summary>
        /// Formatted display of total price
        /// </summary>
        public string FormattedPrice => $"${TotalPrice:F2}";

        /// <summary>
        /// Formatted display of earnings
        /// </summary>
        public string FormattedEarnings => $"${Earnings:F2}";

        /// <summary>
        /// Time received in short format
        /// </summary>
        public string ReceivedTimeText => ReceivedAt.ToString("HH:mm:ss");

        /// <summary>
        /// Expiration time in short format
        /// </summary>
        public string ExpirationTimeText => ExpiresAt.ToString("HH:mm:ss");

        /// <summary>
        /// Short summary for display
        /// </summary>
        public string Summary => $"{CustomerName} - {FileName} ({FormattedPrice})";

        /// <summary>
        /// Whether this offer is still active (not expired)
        /// </summary>
        public bool IsActive => Status == "Active" && DateTime.Now < ExpiresAt;

        /// <summary>
        /// Status color for display
        /// </summary>
        public string StatusColor => Status switch
        {
            "Active" => "#3498db",
            "Accepted" => "#27ae60",
            "Declined" => "#95a5a6",
            "Expired" => "#e74c3c",
            "Cancelled" => "#f39c12",
            _ => "#34495e"
        };
    }
}