using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Ngsoft.Http
{
    public class HttpBuilder
    {
        private static readonly HttpClient _http = new HttpClient();
        private readonly HttpRequestMessage _message;
        private string _body;
        private string _mediaType;
        private Encoding _encoding;
        private MultipartFormDataContent _multipartForm;
        private FormUrlEncodedContent _encodedForm;

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

        public HttpBuilder SetBearerAuthentication(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new ArgumentException("Token cannot be empty.", nameof(token));
            }

            var authorizationHeaderName = "Authorization";
            TryRemoveHeader(authorizationHeaderName);
            _message.Headers.Add(authorizationHeaderName, $"Bearer {token}");
            return this;
        }

        public async Task<HttpResponseMessage> RequestAsync()
        {
            _message.Content = GetContent();
            return await _http.SendAsync(_message);

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
                    new StringContent(content: _body ?? string.Empty, encoding: _encoding ?? Encoding.UTF8) :
                    new StringContent(content: _body ?? string.Empty, encoding: _encoding ?? Encoding.UTF8, mediaType: _mediaType);
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
