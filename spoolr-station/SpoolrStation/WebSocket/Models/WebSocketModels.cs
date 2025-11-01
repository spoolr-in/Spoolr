using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace SpoolrStation.WebSocket.Models
{
    // ======================== WEBSOCKET MESSAGE MODELS ========================

    /// <summary>
    /// Job offer message received via WebSocket from Spoolr backend
    /// Contains complete job information and 90-second expiry countdown
    /// </summary>
    public class JobOfferMessage
    {
        [JsonProperty("type")]
        [JsonPropertyName("type")]
        public string Type { get; set; } = "NEW_JOB_OFFER";

        [JsonProperty("jobId")]
        [JsonPropertyName("jobId")]
        public long JobId { get; set; }

        [JsonProperty("trackingCode")]
        [JsonPropertyName("trackingCode")]
        public string TrackingCode { get; set; } = string.Empty;

        [JsonProperty("fileName")]
        [JsonPropertyName("fileName")]
        public string FileName { get; set; } = string.Empty;

        [JsonProperty("customer")]
        [JsonPropertyName("customer")]
        public string Customer { get; set; } = string.Empty;

        [JsonProperty("printSpecs")]
        [JsonPropertyName("printSpecs")]
        public string PrintSpecs { get; set; } = string.Empty;

        [JsonProperty("totalPrice")]
        [JsonPropertyName("totalPrice")]
        public decimal TotalPrice { get; set; }

        [JsonProperty("earnings")]
        [JsonPropertyName("earnings")]
        public decimal Earnings { get; set; }

        [JsonProperty("createdAt")]
        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonProperty("isAnonymous")]
        [JsonPropertyName("isAnonymous")]
        public bool IsAnonymous { get; set; }

        [JsonProperty("offerExpiresInSeconds")]
        [JsonPropertyName("offerExpiresInSeconds")]
        public int OfferExpiresInSeconds { get; set; } = 90;

        // Computed properties for UI
        public DateTime ExpiresAt 
        {
            get
            {
                // If CreatedAt wasn't properly deserialized (remains default), use current time
                var baseTime = CreatedAt == default(DateTime) ? DateTime.UtcNow : CreatedAt;
                return baseTime.AddSeconds(OfferExpiresInSeconds);
            }
        }
        public string DisplayCustomer => IsAnonymous ? "Anonymous Customer" : Customer;
        public string FormattedEarnings => $"${Earnings:F2}";
        public string FormattedPrice => $"${TotalPrice:F2}";
    }

    /// <summary>
    /// Job offer cancellation message when offer is no longer available
    /// </summary>
    public class JobOfferCancelledMessage
    {
        [JsonProperty("type")]
        public string Type { get; set; } = "OFFER_CANCELLED";

        [JsonProperty("jobId")]
        public long JobId { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Base class for all WebSocket message types
    /// </summary>
    public abstract class WebSocketMessage
    {
        [JsonProperty("type")]
        public abstract string Type { get; }

        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public DateTime ReceivedAt { get; set; } = DateTime.UtcNow;
    }

    // ======================== STOMP PROTOCOL MODELS ========================

    /// <summary>
    /// STOMP frame structure for protocol communication
    /// </summary>
    public class StompFrame
    {
        public string Command { get; set; } = string.Empty;
        public Dictionary<string, string> Headers { get; set; } = new();
        public string Body { get; set; } = string.Empty;

        public StompFrame() { }

        public StompFrame(string command, string body = "", Dictionary<string, string>? headers = null)
        {
            Command = command;
            Body = body ?? string.Empty;
            Headers = headers ?? new Dictionary<string, string>();
        }

        /// <summary>
        /// Serialize STOMP frame to string format for transmission
        /// </summary>
        public string Serialize()
        {
            var frame = Command + "\n";
            
            foreach (var header in Headers)
            {
                frame += $"{header.Key}:{header.Value}\n";
            }
            
            frame += "\n" + Body + "\0";
            return frame;
        }

        /// <summary>
        /// Parse STOMP frame from string format
        /// </summary>
        public static StompFrame Parse(string frameData)
        {
            var lines = frameData.Split('\n');
            var frame = new StompFrame
            {
                Command = lines[0]
            };

            int bodyStartIndex = 1;
            for (int i = 1; i < lines.Length; i++)
            {
                if (string.IsNullOrEmpty(lines[i]))
                {
                    bodyStartIndex = i + 1;
                    break;
                }

                var colonIndex = lines[i].IndexOf(':');
                if (colonIndex > 0)
                {
                    var key = lines[i].Substring(0, colonIndex);
                    var value = lines[i].Substring(colonIndex + 1);
                    frame.Headers[key] = value;
                }
            }

            if (bodyStartIndex < lines.Length)
            {
                frame.Body = string.Join("\n", lines, bodyStartIndex, lines.Length - bodyStartIndex)
                    .TrimEnd('\0');
            }

            return frame;
        }
    }

    /// <summary>
    /// WebSocket connection state enumeration
    /// </summary>
    public enum WebSocketConnectionState
    {
        Disconnected,
        Connecting,
        Connected,
        Reconnecting,
        Failed
    }

    /// <summary>
    /// Job offer status for lifecycle management
    /// </summary>
    public enum JobOfferStatus
    {
        Active,
        Expired,
        Cancelled,
        Accepted,
        Declined
    }

    // ======================== EVENT ARGUMENT MODELS ========================

    /// <summary>
    /// Event arguments for job offer received events
    /// </summary>
    public class JobOfferReceivedEventArgs : EventArgs
    {
        public JobOfferMessage Offer { get; }
        public DateTime ReceivedAt { get; }

        public JobOfferReceivedEventArgs(JobOfferMessage offer)
        {
            Offer = offer;
            ReceivedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Event arguments for job offer cancelled events
    /// </summary>
    public class JobOfferCancelledEventArgs : EventArgs
    {
        public long JobId { get; }
        public string Reason { get; }
        public DateTime CancelledAt { get; }

        public JobOfferCancelledEventArgs(long jobId, string reason)
        {
            JobId = jobId;
            Reason = reason;
            CancelledAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Event arguments for connection status changes
    /// </summary>
    public class ConnectionStatusEventArgs : EventArgs
    {
        public WebSocketConnectionState PreviousState { get; }
        public WebSocketConnectionState CurrentState { get; }
        public string? Message { get; }
        public Exception? Exception { get; }
        public DateTime ChangedAt { get; }

        public ConnectionStatusEventArgs(
            WebSocketConnectionState previousState, 
            WebSocketConnectionState currentState,
            string? message = null,
            Exception? exception = null)
        {
            PreviousState = previousState;
            CurrentState = currentState;
            Message = message;
            Exception = exception;
            ChangedAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Event arguments for message processing errors
    /// </summary>
    public class MessageErrorEventArgs : EventArgs
    {
        public string RawMessage { get; }
        public Exception Exception { get; }
        public DateTime ErrorAt { get; }

        public MessageErrorEventArgs(string rawMessage, Exception exception)
        {
            RawMessage = rawMessage;
            Exception = exception;
            ErrorAt = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Event arguments for job offer response events (accept/decline/expire)
    /// </summary>
    public class JobOfferResponseEventArgs : EventArgs
    {
        public long JobId { get; }
        public string Response { get; }
        public DateTime RespondedAt { get; }

        public JobOfferResponseEventArgs(long jobId, string response)
        {
            JobId = jobId;
            Response = response;
            RespondedAt = DateTime.UtcNow;
        }
    }

    // ======================== VALIDATION MODELS ========================

    /// <summary>
    /// Result of message validation
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; }
        public List<string> Errors { get; }

        public ValidationResult(bool isValid, List<string> errors)
        {
            IsValid = isValid;
            Errors = errors ?? new List<string>();
        }

        public static ValidationResult Success() => new(true, new List<string>());
        public static ValidationResult Failure(params string[] errors) => new(false, new List<string>(errors));
    }

    // ======================== UI DISPLAY MODELS ========================
    // JobOfferDisplayModel is defined separately in Models/JobOfferDisplayModel.cs

    /// <summary>
    /// Base class for property change notifications
    /// </summary>
    public abstract class BaseNotifyPropertyChanged : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value,
            [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
