﻿using System;

namespace JayoOBSPlugin.OBSWebsocketDotNet
{
    /// <summary>
    /// Thrown if authentication fails
    /// </summary>
    public class AuthFailureException : Exception
    {
    }

    /// <summary>
    /// Thrown when the server responds with an error
    /// </summary>
    public class ErrorResponseException : Exception
    {
        /// <summary>
        /// Error Code of exception
        /// </summary>
        public int ErrorCode { get; set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="message">Exception Message</param>
        /// /// <param name="errorCode">Error Code</param>
        public ErrorResponseException(string message, int errorCode) : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}