using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Ngsoft.Http
{
    public class HttpBuilder
    {
        private const string HTTP_REQUEST_FAILED = "HTTP request failed.";
        private const string AUTHORIZATION_HEADER_NAME = "Authorization";
        private static readonly HttpClient _http = new HttpClient();
        private readonly HttpRequestMessage _message;
        private string _body = string.Empty;
        private string _mediaType;
        private Encoding _encoding = Encoding.UTF8;
        private MultipartFormDataContent _multipartForm;
        private FormUrlEncodedContent _encodedForm;
        private int _timeout = 20000;

        /// <summary>
        /// Creates new <see cref="HttpBuilder"/>.
        /// </summary>
        /// <param name="url">The <see cref="Uri"/> for request.</param>
        /// <exception cref="ArgumentNullException"><paramref name="url"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="url"/> is not HTTP or HTTPS scheme.</exception>
        public HttpBuilder(Uri url)
        {
            if (url == null)
            {
                throw new ArgumentNullException(nameof(url));
            }

            _message = (url.Scheme == Uri.UriSchemeHttp || url.Scheme == Uri.UriSchemeHttps) ?
                new HttpRequestMessage(method: HttpMethod.Get, requestUri: url) :
                throw new ArgumentException("URI has to be accessed through HTTP or HTTPS only.", nameof(url));
        }

        /// <summary>
        /// Creates new <see cref="HttpBuilder"/>.
        /// </summary>
        /// <param name="url">The <see cref="Uri"/> for request.</param>
        /// <exception cref="ArgumentNullException"><paramref name="url"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException"><paramref name="url"/> is not HTTP or HTTPS scheme.</exception>
        public static HttpBuilder Create(Uri url)
        {
            return new HttpBuilder(url);
        }

        /// <summary>
        /// Sets method for request. Default value is <see cref="HttpMethod.Get"/>.
        /// </summary>
        /// <param name="method">Request method.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="method"/> is <c>null</c>.</exception>
        public HttpBuilder SetMethod(HttpMethod method)
        {
            _message.Method = method ?? throw new ArgumentNullException(nameof(method));
            return this;
        }

        /// <summary>
        /// Sets request body.
        /// </summary>
        /// <param name="body">Request body.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="body"/> is <c>null</c>.</exception>
        public HttpBuilder SetBody(string body)
        {
            _body = body ?? throw new ArgumentNullException(nameof(body));
            return this;
        }

        /// <summary>
        /// Sets request body media type.
        /// </summary>
        /// <param name="mediaType">Request body media type.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"><paramref name="mediaType"/> is <c>null</c>, empty or whitespace only.</exception>
        public HttpBuilder SetMediaType(string mediaType)
        {
            _mediaType = string.IsNullOrWhiteSpace(mediaType) == false ?
                mediaType : throw new ArgumentException("Media type cannot be empty.", nameof(mediaType));
            return this;
        }

        /// <summary>
        /// Sets request body content encoding. Default value is <see cref="Encoding.UTF8"/>.
        /// </summary>
        /// <param name="encoding">Request body content encoding.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="encoding"/>is <c>null</c>.</exception>
        public HttpBuilder SetEncoding(Encoding encoding)
        {
            _encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
            return this;
        }

        /// <summary>
        /// Sets request content as <c>multipart/form-data</c> (if it has not been set already) and adds form field.
        /// </summary>
        /// <param name="name">Field name.</param>
        /// <param name="value">Field value.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"><paramref name="name"/> is <c>null</c>, empty or whitespace only.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        public HttpBuilder SetFormField(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Form field name cannot be empty.", nameof(name));
            }
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            var content = new StringContent(value);
            if (_multipartForm == null)
            {
                _multipartForm = new MultipartFormDataContent();
            }
            _multipartForm.Add(content, name);
            return this;
        }

        /// <summary>
        /// Sets request content as <c>application/x-www-form-urlencoded</c>.
        /// </summary>
        /// <param name="values">Form data.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="values"/> is <c>null</c>.</exception>
        public HttpBuilder SetEncodedForm(IEnumerable<KeyValuePair<string, string>> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            _encodedForm = new FormUrlEncodedContent(values);
            return this;
        }

        /// <summary>
        /// Sets custom request header.
        /// </summary>
        /// <param name="name">Header name.</param>
        /// <param name="value">Header value.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"><paramref name="name"/> is <c>null</c>, empty or whitespace only.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
        public HttpBuilder SetHeader(string name, string value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Header name cannot be empty.", nameof(name));
            }
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            TryRemoveHeader(name);
            _message.Headers.Add(name, value);
            return this;
        }

        /// <summary>
        /// Sets <c>Basic</c> authorization header with provided data.
        /// </summary>
        /// <param name="username">Username.</param>
        /// <param name="password">Password.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"><paramref name="username"/> or <paramref name="password"/> is <c>null</c>.</exception>
        public HttpBuilder SetBasicAuthorization(string username, string password)
        {
            if (username == null)
            {
                throw new ArgumentNullException(nameof(username));
            }
            if (password == null)
            {
                throw new ArgumentNullException(nameof(password));
            }

            TryRemoveHeader(AUTHORIZATION_HEADER_NAME);
            var value = Encoding
                .GetEncoding("ISO-8859-1")
                .GetBytes($"{username}:{password}");
            _message.Headers.Add(AUTHORIZATION_HEADER_NAME, $"Basic {Convert.ToBase64String(value)}");
            return this;
        }

        /// <summary>
        /// Sets <c>Bearer</c> authorization header with provided token value.
        /// </summary>
        /// <param name="token">Token.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"><paramref name="token"/> is <c>null</c>.</exception>
        public HttpBuilder SetBearerAuthentication(string token)
        {
            if (token == null)
            {
                throw new ArgumentNullException(nameof(token));
            }

            TryRemoveHeader(AUTHORIZATION_HEADER_NAME);
            _message.Headers.Add(AUTHORIZATION_HEADER_NAME, $"Bearer {token}");
            return this;
        }

        /// <summary>
        /// Sets request timeout. Default value is 20000 (20 seconds). Note that using <see cref="HttpClient"/> timeout value is 100000 (100 seconds) so if greater value will be provided then 100000 value will be used.
        /// </summary>
        /// <param name="timeout">Timeout value.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeout"/> value is negative or zero.</exception>
        public HttpBuilder SetTimeout(int timeout)
        {
            if (timeout <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout value cannot be negative or zero.");
            }

            _timeout = timeout;
            return this;
        }

        /// <summary>
        /// Processes request with provided options. If selected <see cref="HttpMethod"/> is <see cref="HttpMethod.Get"/> then request content will be <c>null</c>. Else if <see cref="MultipartFormDataContent"/> provided, then request will use it. Else if <see cref="FormUrlEncodedContent"/> provided, then request will use it. Finally if there is no form content will be used <see cref="StringContent"/> with provided <see cref="Encoding"/> and media type (if any).
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">HTTP request failed.</exception>
        public async Task<HttpResponseMessage> RequestAsync()
        {
            _message.Content = GetContent();
            var cts = new CancellationTokenSource(_timeout);
            try
            {
                return await _http.SendAsync(_message, cts.Token);
            }
            catch (HttpRequestException e)
            {
                throw new InvalidOperationException(HTTP_REQUEST_FAILED, e);
            }
            catch (TaskCanceledException e)
            {
                throw new InvalidOperationException(HTTP_REQUEST_FAILED, e);
            }

            HttpContent GetContent()
            {
                if (_message.Method == HttpMethod.Get)
                {
                    return null;
                }
                if (_multipartForm != null)
                {
                    return _multipartForm;
                }
                if (_encodedForm != null)
                {
                    return _encodedForm;
                }
                return _mediaType == null ?
                    new StringContent(_body, _encoding) :
                    new StringContent(_body, _encoding, _mediaType);
            }
        }

        #region private

        private void TryRemoveHeader(string name)
        {
            var header = _message
                .Headers
                .FirstOrDefault(h => h.Key.ToUpper() == name.ToUpper());
            if (header.Key != null)
            {
                _message.Headers.Remove(header.Key);
            }
        }

        #endregion
    }
}
