using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

namespace cAlgo
{

    public class Telegram
    {

        public Telegram()
        {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        public string SendTelegram(string chatId, string token, string telegramMessage)
        {

            string reply = string.Empty;
            long id = Convert.ToInt64(chatId);

            try
            {
                var bot = new TelegramBotClient(token);
                bot.SendTextMessageAsync(id, telegramMessage);
                reply = "Success";

            }
            catch (Exception ex)
            {
                reply = "ERROR: " + ex.Message;
            }

            return reply;
        }
    }

}
