using System;
using SpoolrStation.WebSocket.Models;

namespace SpoolrStation.Models
{
    /// <summary>
    /// Model representing an accepted job in the vendor's queue
    /// </summary>
    public class AcceptedJobModel
    {
        /// <summary>
        /// Unique job identifier
        /// </summary>
        public long JobId { get; set; }

        /// <summary>
        /// Customer name or identifier
        /// </summary>
        public string Customer { get; set; } = string.Empty;

        /// <summary>
        /// Name of the file to be printed
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// Print specifications and requirements
        /// </summary>
        public string PrintSpecs { get; set; } = string.Empty;

        /// <summary>
        /// Total price customer pays
        /// </summary>
        public decimal TotalPrice { get; set; }

        /// <summary>
        /// Vendor earnings from this job
        /// </summary>
        public decimal Earnings { get; set; }

        /// <summary>
        /// When the job was originally created
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// When the job was accepted by this vendor
        /// </summary>
        public DateTime AcceptedAt { get; set; }

        /// <summary>
        /// Whether the customer is anonymous
        /// </summary>
        public bool IsAnonymous { get; set; }

        /// <summary>
        /// Current status of the accepted job
        /// </summary>
        public string Status { get; set; } = "Accepted";

        /// <summary>
        /// Job tracking code
        /// </summary>
        public string TrackingCode { get; set; } = string.Empty;

        // ===== DISPLAY PROPERTIES =====

        /// <summary>
        /// Display customer name (handles anonymous customers)
        /// </summary>
        public string DisplayCustomer => IsAnonymous ? "Anonymous Customer" : Customer;

        /// <summary>
        /// Formatted display of total price
        /// </summary>
        public string FormattedPrice => $"${TotalPrice:F2}";

        /// <summary>
        /// Formatted display of earnings
        /// </summary>
        public string FormattedEarnings => $"${Earnings:F2}";

        /// <summary>
        /// Combined price and earnings display
        /// </summary>
        public string PriceDisplay => $"Customer pays: {FormattedPrice} • You earn: {FormattedEarnings}";

        /// <summary>
        /// Job summary for display in lists
        /// </summary>
        public string JobSummary => $"{DisplayCustomer} • {FileName}";

        /// <summary>
        /// Status color for UI display
        /// </summary>
        public string StatusColor => Status switch
        {
            "Accepted" => "#f39c12",      // Orange
            "Printing" => "#3498db",      // Blue
            "Ready" => "#27ae60",         // Green
            "Completed" => "#95a5a6",     // Gray
            "Cancelled" => "#e74c3c",     // Red
            _ => "#34495e"                // Dark gray
        };

        /// <summary>
        /// Creates an AcceptedJobModel from a JobOfferMessage
        /// </summary>
        /// <param name="jobOffer">The original job offer that was accepted</param>
        /// <returns>New AcceptedJobModel instance</returns>
        public static AcceptedJobModel FromJobOffer(JobOfferMessage jobOffer)
        {
            return new AcceptedJobModel
            {
                JobId = jobOffer.JobId,
                Customer = jobOffer.Customer,
                FileName = jobOffer.FileName,
                PrintSpecs = jobOffer.PrintSpecs,
                TotalPrice = jobOffer.TotalPrice,
                Earnings = jobOffer.Earnings,
                CreatedAt = jobOffer.CreatedAt,
                AcceptedAt = DateTime.UtcNow,
                IsAnonymous = jobOffer.IsAnonymous,
                Status = "Accepted",
                TrackingCode = jobOffer.TrackingCode
            };
        }
    }
}