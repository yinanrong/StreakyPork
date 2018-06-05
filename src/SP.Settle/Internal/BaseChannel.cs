using System.Net.Http;
using System.Threading.Tasks;

namespace Sp.Settle.Internal
{
    internal abstract class BaseChannel
    {
        private HttpClient _backChannel;
        private HttpClientHandler _httpHandler;

        protected HttpClient BackChannel => _backChannel ?? (_backChannel = new HttpClient(HttpHandler));

        protected HttpClientHandler HttpHandler =>
            _httpHandler ?? (_httpHandler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (s, cer, cha, e) => true,
                UseProxy = false
            });


        protected async Task<string> PostAsStringAsync(string url, string content)
        {
            using (var resp = await BackChannel.PostAsync(url, new StringContent(content)))
            {
                return await resp.Content.ReadAsStringAsync();
            }
        }

        protected async Task<string> GetAsStringAsync(string url)
        {
            return await BackChannel.GetStringAsync(url);
        }
    }
}