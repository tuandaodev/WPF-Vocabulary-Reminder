using System;
using System.Security.Cryptography;
using System.Text;

namespace VR.Services
{
    /// <summary>
    /// Provides secure encryption and decryption for sensitive data like API keys
    /// Uses Windows Data Protection API (DPAPI) for user-specific encryption
    /// </summary>
    public class SecurityService
    {
        /// <summary>
        /// Encrypts a string using DPAPI with CurrentUser scope
        /// </summary>
        /// <param name="plainText">The plain text to encrypt</param>
        /// <returns>Base64 encoded encrypted string, or null if input is null/empty</returns>
        public static string EncryptString(string plainText)
        {
            if (string.IsNullOrEmpty(plainText))
                return null;

            try
            {
                byte[] plainTextBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encryptedBytes = ProtectedData.Protect(
                    plainTextBytes, 
                    null, // No additional entropy
                    DataProtectionScope.CurrentUser // Encrypt for current user only
                );
                return Convert.ToBase64String(encryptedBytes);
            }
            catch (Exception ex)
            {
                throw new SecurityException($"Failed to encrypt data: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Decrypts a Base64 encoded string that was encrypted using DPAPI
        /// </summary>
        /// <param name="encryptedText">The Base64 encoded encrypted string</param>
        /// <returns>Decrypted plain text, or null if input is null/empty</returns>
        public static string DecryptString(string encryptedText)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return null;

            try
            {
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);
                byte[] decryptedBytes = ProtectedData.Unprotect(
                    encryptedBytes, 
                    null, // No additional entropy
                    DataProtectionScope.CurrentUser // Decrypt for current user only
                );
                return Encoding.UTF8.GetString(decryptedBytes);
            }
            catch (Exception ex)
            {
                throw new SecurityException($"Failed to decrypt data: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Checks if a string appears to be encrypted (Base64 format)
        /// This is a basic check and not foolproof
        /// </summary>
        /// <param name="text">The text to check</param>
        /// <returns>True if the text appears to be encrypted</returns>
        public static bool IsEncrypted(string text)
        {
            if (string.IsNullOrEmpty(text))
                return false;

            try
            {
                Convert.FromBase64String(text);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Custom exception for security-related operations
    /// </summary>
    public class SecurityException : Exception
    {
        public SecurityException(string message) : base(message) { }
        public SecurityException(string message, Exception innerException) : base(message, innerException) { }
    }
}