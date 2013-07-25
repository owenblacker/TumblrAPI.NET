using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace TumblrAPI
{
	internal static class FormUpload
	{
		private static readonly Encoding __encoding = Encoding.UTF8;

		public static HttpWebRequest MultipartFormDataPost(string postUrl, string userAgent, Dictionary<string, object> postParameters)
		{
			const string formDataBoundary = "-----------------------------28947758029299";
			const string contentType = "multipart/form-data; boundary=" + formDataBoundary;

			byte[] formData = GetMultipartFormData(postParameters, formDataBoundary);

			return PostForm(postUrl, userAgent, contentType, formData);
		}

		private static HttpWebRequest PostForm(string postUrl, string userAgent, string contentType, byte[] formData)
		{
			HttpWebRequest request = WebRequest.Create(postUrl) as HttpWebRequest;
			if (request == null)
			{
				throw new NullReferenceException("request is not a http request");
			}

			// Set up the request properties
			request.Method = "POST";
			request.ContentType = contentType;
			request.UserAgent = userAgent;
			request.SendChunked = true;
			request.CookieContainer = new CookieContainer();
			request.ContentLength = formData.Length;  // We need to count how many bytes we're sending. 

			using (Stream requestStream = request.GetRequestStream())
			{
				// Push it out there
				requestStream.Write(formData, 0, formData.Length);
				requestStream.Close();
			}

			return request;
		}

		private static byte[] GetMultipartFormData(Dictionary<string, object> postParameters, string boundary)
		{
			Stream formDataStream = new MemoryStream();

			foreach (var param in postParameters)
			{
				if (param.Value is FileParameter)
				{
					FileParameter fileToUpload = (FileParameter) param.Value;

					// Add just the first part of this param, since we will write the file data directly to the Stream
					string header = String.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"; filename=\"{2}\";\r\nContent-Type: {3}\r\n\r\n",
						boundary, param.Key, fileToUpload.FileName ?? param.Key,
						fileToUpload.ContentType ?? "application/octet-stream");

					formDataStream.Write(__encoding.GetBytes(header), 0, header.Length);

					// Write the file data directly to the Stream, rather than serializing it to a string.
					formDataStream.Write(fileToUpload.File, 0, fileToUpload.File.Length);
					// Thanks to feedback from commenters, add a CRLF to allow multiple files to be uploaded
					formDataStream.Write(__encoding.GetBytes("\r\n"), 0, 2);
				}
				else
				{
					string postData = String.Format("--{0}\r\nContent-Disposition: form-data; name=\"{1}\"\r\n\r\n{2}\r\n",
						boundary,
						param.Key,
						param.Value);
					formDataStream.Write(__encoding.GetBytes(postData), 0, postData.Length);
				}
			}

			// Add the end of the request
			string footer = "\r\n--" + boundary + "--\r\n";
			formDataStream.Write(__encoding.GetBytes(footer), 0, footer.Length);

			// Dump the Stream into a byte[]
			formDataStream.Position = 0;
			byte[] formData = new byte[formDataStream.Length];
			formDataStream.Read(formData, 0, formData.Length);
			formDataStream.Close();

			return formData;
		}
	}
}