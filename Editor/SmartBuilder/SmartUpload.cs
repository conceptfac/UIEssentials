#if UNITY_EDITOR
using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using Renci.SshNet;
using Amazon.S3;
using Amazon.S3.Model;

namespace Concept.Core
{

    /// <summary>
    /// SmartUpload handles file uploads to different targets (SFTP, AWS S3, etc.).
    /// It provides progress and status callbacks for integration with custom editor windows or runtime tools.
    /// </summary>
    public class SmartUpload
    {
        public enum UploadTarget { SFTP, AWSS3 }
        private UploadTarget m_uploadTarget;

        private string m_host;
        private int m_port;
        private string m_user;
        private string m_password;
        private IAmazonS3 m_s3Client;
        private string m_bucketName;

        /// <summary>
        /// Fired when the progress of the upload changes (0..1).
        /// </summary>
        public event Action<string> OnStatusChanged;
        /// <summary>
        /// Fired when the status message changes (technical English log).
        /// </summary>
        public event Action<float> OnProgressChanged; // 0..1

        /// <summary>
        /// Creates a new SFTP SmartUpload instance with required configuration.
        /// </summary>
        /// <param name="host">Host or endpoint address.</param>
        /// <param name="port">Port (default 22 for SFTP).</param>
        /// <param name="user">Username for authentication.</param>
        /// <param name="password">Password for authentication.</param>
        public SmartUpload(string host, int port, string user, string password)
        {
            m_host = host;
            m_port = port;
            m_user = user;
            m_password = password;
            m_uploadTarget = UploadTarget.SFTP;
        }

        /// <summary>
        /// Creates a new AWSS3 SmartUpload instance with required configuration.
        /// </summary>
        /// <param name="amazonS3"></param>
        /// <param name="bucketName"></param>
        public SmartUpload(IAmazonS3 amazonS3, string bucketName)
        {
            m_bucketName = bucketName;
            m_s3Client = amazonS3;
            m_uploadTarget = UploadTarget.AWSS3;
        }

        /// <summary>
        /// Uploads multiple files asynchronously to the configured target.
        /// </summary>
        /// <param name="localPaths">Array of local file paths.</param>
        /// <param name="remoteDir">Remote directory path.</param>
        public async Task UploadFilesAsync(string[] localPaths, string remoteDir)
        {
            int total = localPaths.Length;

            if (m_uploadTarget == UploadTarget.SFTP)
            {
                // Limpa pasta apenas 1 vez
                using var client = new SftpClient(m_host, m_port, m_user, m_password);
                try
                {
                    client.Connect();
                    if (!client.IsConnected)
                    {
                        OnStatusChanged?.Invoke("[SmartUpload] Could not connect to SFTP server.");
                        return;
                    }

                    if (!client.Exists(remoteDir))
                        client.CreateDirectory(remoteDir);

                    await DeleteAsync(client, remoteDir);
                }
                finally
                {
                    client.Disconnect();
                }
            }

            for (int i = 0; i < total; i++)
            {
                string path = localPaths[i];
                OnStatusChanged?.Invoke($"[SmartUpload] Uploading {Path.GetFileName(path)} ({i + 1}/{total})");

                if (m_uploadTarget == UploadTarget.SFTP)
                {
                    await UploadSFTPAsync(path, remoteDir);
                }
                else if (m_uploadTarget == UploadTarget.AWSS3)
                {
                    await UploadAWSAsync(path, remoteDir);
                }

                OnProgressChanged?.Invoke((i + 1f) / total);
            }

            OnStatusChanged?.Invoke("[SmartUpload] Upload finished!");
        }

        /// <summary>
        /// Uploads a single file via SFTP.
        /// </summary>
        /// <param name="localFilePath">Local path of the file to upload.</param>
        /// <param name="remoteDir">Remote directory path.</param>
        private async Task UploadSFTPAsync(string localFilePath, string remoteDir)
        {
            using var client = new SftpClient(m_host, m_port, m_user, m_password);

            try
            {
                client.Connect();
                if (!client.IsConnected)
                {
                    OnStatusChanged?.Invoke("[SmartUpload] Could not connect to SFTP server.");
                    return;
                }

                OnStatusChanged?.Invoke("[SmartUpload] Connected to SFTP server.");
                await SendFileAsync(client, localFilePath, remoteDir);
                OnStatusChanged?.Invoke("[SmartUpload] SFTP upload completed.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SmartUpload] Error in UploadSFTPAsync: {ex.Message}");
            }
            finally
            {
                client.Disconnect();
            }
        }

        /// <summary>
        /// Deletes all files and subdirectories recursively inside a remote directory (SFTP).
        /// </summary>
        /// <param name="client">Connected SFTP client.</param>
        /// <param name="remoteDir">Remote directory path.</param>
        private async Task DeleteAsync(SftpClient client, string remoteDir)
        {
            await Task.Run(() =>
            {
                foreach (var file in client.ListDirectory(remoteDir))
                {
                    if (file.Name == "." || file.Name == "..") continue;

                    if (file.IsDirectory)
                    {
                        DeleteAsync(client, remoteDir + "/" + file.Name).GetAwaiter().GetResult();
                        client.DeleteDirectory(remoteDir + "/" + file.Name);
                    }
                    else
                    {
                        client.DeleteFile(remoteDir + "/" + file.Name);
                    }
                }
            });
        }

        /// <summary>
        /// Sends a single file with upload progress tracking via SFTP.
        /// </summary>
        /// <param name="client">Connected SFTP client.</param>
        /// <param name="localFilePath">Path of the local file.</param>
        /// <param name="remoteDir">Destination directory on the server.</param>
        private Task SendFileAsync(SftpClient client, string localFilePath, string remoteDir)
        {
            return Task.Run(() =>
            {
                var remoteFile = remoteDir + "/" + Path.GetFileName(localFilePath);
                using var fs = new FileStream(localFilePath, FileMode.Open, FileAccess.Read);
                client.UploadFile(fs, remoteFile, uploadedBytes =>
                {
                    float progress = (float)uploadedBytes / fs.Length;
                    OnProgressChanged?.Invoke(progress);
                });
            });
        }

        /// <summary>
        /// Uploads a single file to AWS S3.
        /// </summary>
        /// <param name="localFilePath">Local path of the file to upload.</param>
        /// <param name="remoteDir">Remote directory or S3 bucket path.</param>
        private async Task UploadAWSAsync(string localFilePath, string remoteDir)
        {
            if (m_s3Client == null || string.IsNullOrEmpty(m_bucketName))
            {
                Debug.LogError("[SmartUpload] AWS S3 client or bucket not configured.");
                return;
            }

            try
            {
                using FileStream fs = new FileStream(localFilePath, FileMode.Open, FileAccess.Read);
                string fileName = Path.GetFileName(localFilePath);
                string keyName = $"{remoteDir.TrimEnd('/')}/{fileName}";

                var request = new PutObjectRequest
                {
                    BucketName = m_bucketName,
                    Key = keyName,
                    InputStream = fs,
                    ContentType = GetContentType(fileName)
                };

                if (fileName.EndsWith(".br"))
                    request.Headers["Content-Encoding"] = "br";

                await m_s3Client.PutObjectAsync(request);
                OnStatusChanged?.Invoke($"[SmartUpload] AWS upload completed: {keyName}");
            }
            catch (AmazonS3Exception e)
            {
                Debug.LogError($"[SmartUpload] AWS S3 upload error {localFilePath}: {e.Message}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[SmartUpload] Unknown error uploading {localFilePath}: {e.Message}");
            }
        }

                /// <summary>
        /// Resolves the correct MIME Content-Type for a given file extension.
        /// </summary>
        /// <param name="fileName">File name to check.</param>
        /// <returns>Content-Type string.</returns>
        private static string GetContentType(string fileName)
        {
            string ext = Path.GetExtension(fileName).ToLower();
            return ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".json" => "application/json",
                ".txt" => "text/plain",
                ".js" => "application/javascript",
                ".html" => "text/html",
                ".css" => "text/css",
                ".br" => "application/octet-stream",
                _ => "application/octet-stream",
            };
        }
    }
}
#endif
