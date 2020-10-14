﻿using System;
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

        public static HttpBuilder Create(Uri url)
        {
            return new HttpBuilder(url);
        }

        public HttpBuilder SetMethod(HttpMethod method)
        {
            _message.Method = method ?? throw new ArgumentNullException(nameof(method));
            return this;
        }

        public HttpBuilder SetBody(string body)
        {
            _body = body ?? throw new ArgumentNullException(nameof(body));
            return this;
        }

        public HttpBuilder SetMediaType(string mediaType)
        {
            _mediaType = string.IsNullOrWhiteSpace(mediaType) == false ?
                mediaType : throw new ArgumentException("Media type cannot be empty.", nameof(mediaType));
            return this;
        }

        public HttpBuilder SetEncoding(Encoding encoding)
        {
            _encoding = encoding ?? throw new ArgumentNullException(nameof(encoding));
            return this;
        }

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

        public HttpBuilder SetEncodedForm(IEnumerable<KeyValuePair<string, string>> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            _encodedForm = new FormUrlEncodedContent(values);
            return this;
        }

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

        public HttpBuilder SetBearerAuthentication(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Token cannot be empty.", nameof(token));
            }

            TryRemoveHeader(AUTHORIZATION_HEADER_NAME);
            _message.Headers.Add(AUTHORIZATION_HEADER_NAME, $"Bearer {token}");
            return this;
        }

        public HttpBuilder SetTimeout(int timeout)
        {
            if (timeout <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), "Timeout value cannot be negative or zero.");
            }

            _timeout = timeout;
            return this;
        }

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
