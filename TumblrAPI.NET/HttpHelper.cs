using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Web;

namespace TumblrAPI
{
	/// <summary>
	/// Submits post data to a url.
	/// </summary>
	internal class HttpHelper
	{
		private readonly IDictionary<string, string> _myPostItems = new Dictionary<string, string>();
		private string _myUrl;

		/// <summary>
		/// Constructor allowing the setting of the url and items to post.
		/// </summary>
		/// <param name="url">the url for the post.</param>
		/// <param name="values">The values for the post.</param>
		public HttpHelper(string url, IDictionary<string, string> values)
		{
			_myUrl = url;
			_myPostItems = values;
		}

		/// <summary>
		/// determines what type of post to perform.
		/// </summary>
		public enum PostType
		{
			Get,
			Post
		}

		/// <summary>
		/// Gets or sets the url to submit the post to.
		/// </summary>
		public string Url
		{
			get { return _myUrl; }
			set { _myUrl = value; }
		}

		/// <summary>
		/// Posts the supplied data to specified url.
		/// </summary>
		/// <returns>a string containing the result of the post.</returns>
		public TumblrResult Post()
		{
			return PostData(_myUrl);
		}

		private string EncodePostItems()
		{
			StringBuilder sb = new StringBuilder();
			foreach (var item in _myPostItems)
			{
				if (item.Key != PostItemParameters.Data)
				{
					sb.Append(item.Key);
					sb.Append('=');
					sb.Append(HttpUtility.UrlEncode(item.Value));
					sb.Append('&');
				}
			}
			return sb.ToString().TrimEnd('&');
		}

		/// <summary>
		/// Posts data to a specified url. Note that this assumes that you have already url encoded the post data.
		/// </summary>
		/// <param name="url">the url to post to.</param>
		/// <returns>Returns the result of the post.</returns>
		private TumblrResult PostData(string url)
		{
			Uri uri = new Uri(url);
			HttpWebRequest request = (HttpWebRequest) WebRequest.Create(uri);
			request.Method = "POST";

			if (_myPostItems.ContainsKey(PostItemParameters.Data))
			{
				string filename = _myPostItems[PostItemParameters.Data];
				byte[] data;
				// Read file data
				using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
				{
					data = new byte[fs.Length];
					fs.Read(data, 0, data.Length);
					fs.Close();
				}
				_myPostItems.Remove(PostItemParameters.Data);
				// Generate post objects
				var postParameters = new Dictionary<string, object>();
				foreach (var item in _myPostItems)
				{
					postParameters.Add(item.Key, item.Value);
				}
				postParameters.Add("data", new FileParameter(data));

				// Create request and receive response
				request = FormUpload.MultipartFormDataPost(_myUrl, "TumblrAPI.NET", postParameters);
			}
			else
			{
				byte[] postData = new UTF8Encoding().GetBytes(EncodePostItems());
				request.ContentType = "application/x-www-form-urlencoded";
				request.ContentLength = postData.Length;
				using (Stream writeStream = request.GetRequestStream())
				{
					writeStream.Write(postData, 0, postData.Length);
				}
			}

			try
			{
				HttpWebResponse response = (HttpWebResponse) request.GetResponse();
				// ReSharper disable AssignNullToNotNullAttribute
				StreamReader stream = new StreamReader(response.GetResponseStream(), true);
				// ReSharper restore AssignNullToNotNullAttribute
				string content = stream.ReadToEnd();
				response.Close();
				return new TumblrResult
				{
					Message = content,
					PostStatus = PostStatus.Created
				};
			}
			catch (WebException ex)
			{
				PostStatus status;
				// ReSharper disable AssignNullToNotNullAttribute
				StreamReader stream = new StreamReader(ex.Response.GetResponseStream(), true);
				// ReSharper restore AssignNullToNotNullAttribute
				string content = stream.ReadToEnd();
				ex.Response.Close();
				HttpStatusCode httpStatusCode = ((HttpWebResponse) ex.Response).StatusCode;
				switch (httpStatusCode)
				{
					case HttpStatusCode.OK:
					case HttpStatusCode.Created:
						status = PostStatus.Created;
						break;
					case HttpStatusCode.Unauthorized:
					case HttpStatusCode.Forbidden:
						status = PostStatus.Forbidden;
						break;
					case HttpStatusCode.NotFound:
					case HttpStatusCode.BadRequest:
					default:
						status = PostStatus.BadRequest;
						break;
				}

				return new TumblrResult
				{
					Message = content,
					PostStatus = status
				};
			}
			catch (Exception ex)
			{
				return new TumblrResult
				{
					Message = ex.ToString(),
					PostStatus = PostStatus.Unknown
				};
			}
		}
	}
}
