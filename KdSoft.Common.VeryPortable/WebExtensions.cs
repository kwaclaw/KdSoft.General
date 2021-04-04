using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace KdSoft.Common
{
  public static class WebExtensions
  {
    public static WebResponse EndGetResponseEx(this WebRequest request, IAsyncResult asyncResult) {
      try {
        return request.EndGetResponse(asyncResult);
      }
      catch (WebException wex) {
        if (wex.Response != null) {
          return wex.Response;
        }
        throw;
      }
    }

    public static string ReadContentString(this HttpWebResponse response) {
      var stream = response.GetResponseStream();
      using (var streamReader = new StreamReader(stream)) {
        return streamReader.ReadToEnd();
      }
    }

    public static bool IsSuccessStatusCode(this HttpWebResponse response) {
      int statusValue = (int)response.StatusCode;
      return statusValue >= 200 && statusValue <= 299;
    }

    public static Task WriteContentStringAsync(this HttpWebRequest request, string content, Encoding encoding) {
      var streamTask = Task<Stream>.Factory.FromAsync(request.BeginGetRequestStream, request.EndGetRequestStream, null);
      return streamTask.ContinueWith(st => {
        using (var streamWriter = new StreamWriter(st.Result, encoding)) {
          streamWriter.Write(content);
          streamWriter.Flush();
        }
      });
    }
  }
}
