using SpoolrStation.WebSocket.Models;
using System;
using System.Threading.Tasks;

namespace SpoolrStation.Services
{
    /// <summary>
    /// Service for generating test job offers to debug the UI
    /// </summary>
    public class TestJobOfferService
    {
        public static JobOfferMessage CreateTestJobOffer()
        {
            return new JobOfferMessage
            {
                Type = "NEW_JOB_OFFER",
                JobId = 12345,
                TrackingCode = "PJ123456",
                FileName = "Resume_Document.pdf",
                Customer = "John Doe",
                PrintSpecs = "A4, Color, Double-sided, 3 copies",
                TotalPrice = 15.50m,
                Earnings = 12.75m,
                CreatedAt = DateTime.UtcNow,
                IsAnonymous = false,
                OfferExpiresInSeconds = 90
            };
        }

        public static JobOfferMessage CreateTestAnonymousJobOffer()
        {
            return new JobOfferMessage
            {
                Type = "NEW_JOB_OFFER",
                JobId = 67890,
                TrackingCode = "PJ789012",
                FileName = "Presentation_Slides.pptx",
                Customer = "",
                PrintSpecs = "A3, Black & White, Single-sided, 1 copy",
                TotalPrice = 8.25m,
                Earnings = 6.50m,
                CreatedAt = DateTime.UtcNow,
                IsAnonymous = true,
                OfferExpiresInSeconds = 90
            };
        }
    }
}