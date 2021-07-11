using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Web;
using System.Windows.Forms;
using System.Net.Http;
using System.Linq;

namespace PlayerTest
{
    class LastFM
    {
        private string ApiKey = "";
        private string mySecret = "";
		private string sessionKey;
		private static readonly HttpClient client = new HttpClient();

		public LastFM()
        {

        }

		public void SetSession(string s)
        {
			sessionKey = s;
        }
        public void GetSession()
        {
			// создаём объект HttpWebRequest через статический метод Create класса WebRequest, явно приводим результат к HttpWebRequest. В параметрах указываем страницу, которая указана в API, в качестве параметров - method=auth.gettoken и наш API Key
			HttpWebRequest tokenRequest = (HttpWebRequest)WebRequest.Create("http://ws.audioscrobbler.com/2.0/?method=auth.gettoken&api_key=" + ApiKey);

			// получаем ответ сервера
			HttpWebResponse tokenResponse = (HttpWebResponse)tokenRequest.GetResponse();

			// и полностью считываем его в строку
			string tokenResult = new StreamReader(tokenResponse.GetResponseStream(), Encoding.UTF8).ReadToEnd();

			// извлекаем то, что нам нужно. Можно сделать и через парсинг XML (видимо, я о нём ещё не знал в тот момент, когда писал этот код).
			string token = String.Empty;
			for (int i = tokenResult.IndexOf("<token>") + 7; i < tokenResult.IndexOf("</token"); i++)
			{
				token += tokenResult[i];
			}

			// запускаем в браузере по умолчанию страницу http://www.last.fm/api/auth/ c параметрами API Key и только что полученным токеном)
			OpenUrl("http://www.last.fm/api/auth/?api_key=" + ApiKey + "&token=" + token);

			// запускается страница, где у пользователя спрашивается, можно ли разрешить данному приложению доступ к профилю.

			// ждём подтверждения от пользователя
			DialogResult d = MessageBox.Show("Вы подтвердили доступ?", "Подтверждение", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);

			// если пользователь дал согласие
			if (d == DialogResult.OK)
			{
				// создаём сигнатуру для получения сессии (указываем API Key, метод, токен и наш секретный ключ, всё это без символов '&' и '='
				string tmp = "api_key" + ApiKey + "methodauth.getsessiontoken" + token + mySecret;

				// хешируем это алгоритмом MD5 (думаю, у вас не будет проблем найти его в Интернете)
				//string sig = System.Security.Cryptography.MD5(tmp);
				string sig = MD5(tmp);

				// получаем сессию похожим способом
				HttpWebRequest sessionRequest = (HttpWebRequest)WebRequest.Create("http://ws.audioscrobbler.com/2.0/?method=auth.getsession&token=" + token + "&api_key=" + ApiKey + "&api_sig=" + sig);
				// уже не помню, зачем это свойство выставлять в true, но это обязательно. Зачем-то им нужно перенаправление.
				sessionRequest.AllowAutoRedirect = true;

				// получаем ответ
				HttpWebResponse sessionResponse = (HttpWebResponse)sessionRequest.GetResponse();
				string sessionResult = new StreamReader(sessionResponse.GetResponseStream(),
											   Encoding.UTF8).ReadToEnd();

				// извлечение сессии (опять же проще использовать XML парсер)
				for (int i = sessionResult.IndexOf("<key>") + 5; i < sessionResult.IndexOf("</key>"); i++)
				{
					sessionKey += sessionResult[i];
                    if (File.Exists("session.txt"))
                    {
						File.Delete("session.txt");
                    }
					File.WriteAllText("session.txt", sessionKey);
				}
			}
		}

		public async void NewScrobble(string track, string artist, string album)
        {
			TimeSpan rtime = DateTime.Now - (new DateTime(1970, 1, 1, 0, 0, 0));
			TimeSpan t1 = new TimeSpan(3, 0, 0);
			rtime -= t1; // вычитаем три часа, чтобы не было несоответствия из-за разницы в часовых поясах
						 // получаем количество секунд
			int timestamp = (int)rtime.TotalSeconds;

			var values = new Dictionary<string, string>
				{
					{ "method", "track.scrobble" },
					{ "artist", artist },
					{ "track", track },
					{ "timestamp", timestamp.ToString() },
					{ "api_key", ApiKey },
					{ "sk", sessionKey },

				};
			//&api_sig=KJHJKSDH878797
			//var content = new FormUrlEncodedContent(values);

			//         var response = await client.PostAsync("http://www.example.com/recepticle.aspx", content.ToString());

			//var responseString = await response.Content.ReadAsStringAsync();
			//string url = GetSignedURI(values, true);
			string s = GetSignedURI(values, false);
			var stringContent = new StringContent(s);
			var response = await client.PostAsync("http://ws.audioscrobbler.com/2.0/?", stringContent);
			var responseString = await response.Content.ReadAsStringAsync();
		}






		public void ScrobbleTrack(string track, string artist, string album)
        {
			// узнаем UNIX-время для текущего момента
			TimeSpan rtime = DateTime.Now - (new DateTime(1970, 1, 1, 0, 0, 0));
			TimeSpan t1 = new TimeSpan(3, 0, 0);
			rtime -= t1; // вычитаем три часа, чтобы не было несоответствия из-за разницы в часовых поясах
						 // получаем количество секунд
			int timestamp = (int)rtime.TotalSeconds;

			// формируем строку запроса
			string submissionReqString = String.Empty;

			//добавляем параметры (указываем метод, сессию и API Key):
			submissionReqString += "method=track.scrobble&sk=" + sessionKey + "&api_key=" + ApiKey;

			// добавляем только обязательную информацию о треке (исполнитель, трек, время прослушивания, альбом), кодируя их с помощью статического метода UrlEncode класса HttpUtility.
			submissionReqString += "&artist=" + HttpUtility.UrlEncode(artist);
			submissionReqString += "&track=" + HttpUtility.UrlEncode(track);
			submissionReqString += "& timestamp=" + timestamp.ToString(); // в этой строке не должно быть пробела между & и t. Просто почему-то Хабр неправильно отображает этот участок, если пробел убрать.
			submissionReqString += "&album=" + HttpUtility.UrlEncode(album);

			// формируем сигнатуру (параметры должны записываться сплошняком (без символов '&' и '=' и в алфавитном порядке):
			string signature = String.Empty;

			// сначала добавляем альбом
			signature += "album" + album;

			// потом API Key
			signature += "api_key" + ApiKey;

			// исполнитель		   
			signature += "artist" + artist;

			// метод и ключ сессии
			signature += "methodtrack.scrobblesk" + sessionKey;

			// время
			signature += "timestamp" + timestamp;

			// имя трека
			signature += "track" + track;

			// добавляем секретный код в конец
			signature += mySecret;

			// добавляем сформированную и захешированную MD5 сигнатуру к строке запроса
			submissionReqString += "&api_sig=" + MD5(signature);
			
			// и на этот раз делаем POST запрос на нужную страницу
			HttpWebRequest submissionRequest = (HttpWebRequest)WebRequest.Create("http://ws.audioscrobbler.com/2.0/"); // адрес запроса без параметров

			// очень важная строка. Долго я мучался, пока не выяснил, что она обязательно должна быть
			submissionRequest.ServicePoint.Expect100Continue = false;

			// Настраиваем параметры запроса
			submissionRequest.UserAgent = "Mozilla/5.0";
			// Указываем метод отправки данных скрипту, в случае с POST обязательно
			submissionRequest.Method = "POST";
			// В случае с POST обязательная строка
			submissionRequest.ContentType = "application/x-www-form-urlencoded";

			// ставим таймаут, чтобы программа не повисла при неудаче обращения к серверу, а выкинула Exception
			submissionRequest.Timeout = 20000;

			// Преобразуем данные в соответствующую кодировку, получаем массив байтов из строки с параметрами (UTF8 обязательно)
			byte[] EncodedPostParams = Encoding.UTF8.GetBytes(submissionReqString);
			submissionRequest.ContentLength = EncodedPostParams.Length;

			// Записываем данные в поток запроса (массив байтов, откуда начинаем, сколько записываем)
			submissionRequest.GetRequestStream().Write(EncodedPostParams, 0, EncodedPostParams.Length);
			// закрываем поток
			submissionRequest.GetRequestStream().Close();

			// получаем ответ сервера
			HttpWebResponse submissionResponse = (HttpWebResponse)submissionRequest.GetResponse();

			// считываем поток ответа
			string submissionResult = new StreamReader(submissionResponse.GetResponseStream(), Encoding.UTF8).ReadToEnd();
            // разбор полётов. Если ответ не содержит status="ok", то дело плохо, выкидываем Exception и где-нибудь ловим его.
            try
            {
				if (!submissionResult.Contains("status=\"ok\""))
					throw new Exception("Треки не отправлены! Причина - " + submissionResult);
			}
            catch (Exception)
            {

                throw;
            }
			
		}

        private string MD5(string tmp)
        {
			byte[] hash;
			using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
			{
				hash = md5.ComputeHash(Encoding.UTF8.GetBytes(tmp));
			}
			string hashed = BitConverter.ToString(hash).Replace("-", "");
			return hashed;
		}
		private void OpenUrl(string url)
		{
			try
			{
				Process.Start(url);
			}
			catch
			{
				// hack because of this: https://github.com/dotnet/corefx/issues/10361
				if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
				{
					url = url.Replace("&", "^&");
					Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
				}
				else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
				{
					Process.Start("xdg-open", url);
				}
				else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
				{
					Process.Start("open", url);
				}
				else
				{
					throw;
				}
			}
		}



		public string GetSignedURI(Dictionary<string, string> args, bool get)
		{
			var stringBuilder = new StringBuilder();
			if (get)
				stringBuilder.Append("http://ws.audioscrobbler.com/2.0/?");
			foreach (var kvp in args)
				stringBuilder.AppendFormat("{0}={1}&", kvp.Key, kvp.Value);
			stringBuilder.Append("api_sig=" + SignCall(args));
			return stringBuilder.ToString();
		}
		public string SignCall(Dictionary<string, string> args)
		{
			IOrderedEnumerable<KeyValuePair<string, string>> sortedArgs = args.OrderBy(arg => arg.Key);
			string signature =
				sortedArgs.Select(pair => pair.Key + pair.Value).
				Aggregate((first, second) => first + second);
			
			return NewMD5(signature + mySecret);
		}
		public string NewMD5(string toHash)
		{
			byte[] textBytes = Encoding.UTF8.GetBytes(toHash);
			var cryptHandler = new System.Security.Cryptography.MD5CryptoServiceProvider();
			byte[] hash = cryptHandler.ComputeHash(textBytes);
			return hash.Aggregate("", (current, a) => current + a.ToString("x2"));
		}
	}
}
