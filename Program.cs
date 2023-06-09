using Newtonsoft;
using Newtonsoft.Json;
using System.Net;
using System.Text.RegularExpressions;
using System.Text;

using Td = Telegram.Td;
using TdApi = Telegram.Td.Api;

using LinqToWiki;
using LinqToWiki.Generated;
using System.Net.Http;
using System.Threading;
using System.IO;
using System.Globalization;
using System.Reflection;
using IQToolkit;
using Telegram.Td.Api;
using File = System.IO.File;
using static System.Net.Mime.MediaTypeNames;

namespace ConsoleApp3
{
    internal class Program
    {
        static void Main(string[] args)
        {
            File.WriteAllText("log.txt", GetCurrenTime() + ": Process run\n");

            Td.Client.Execute(new TdApi.SetLogVerbosityLevel(0));
            if (Td.Client.Execute(new TdApi.SetLogStream(new TdApi.LogStreamFile("tdlib.log", 1 << 27, false))) is TdApi.Error)
            {
                throw new System.IO.IOException("Write access to the current directory is required");
            }
            new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                Td.Client.Run();
            }).Start();

            _client = CreateTdClient();

            _defaultHandler.OnResult(Td.Client.Execute(new TdApi.GetTextEntities("@telegram /test_command https://telegram.org telegram.me @gif @test")));
            //("Loged IN");
            File.AppendAllText("log.txt", GetCurrenTime() + ": Client created\n");

            //string message = "LouieAus Telegram API: logged in :3";
            //long FurrychatId = -1001476181121;

            while (!_needQuit)
            {
                // await authorization
                _gotAuthorization.Reset();
                _gotAuthorization.WaitOne();

                _client.Send(new TdApi.LoadChats(null, 100), _defaultHandler); // preload main chat list
                while (_haveAuthorization)
                {
                    GetCommand();
                    Thread.Sleep(3500);
                }
            }
            while (!_canQuit)
            {
                Thread.Sleep(1);
            }
        }
        private static long GetChatId(string arg)
        {
            long chatId = 0;
            try
            {
                chatId = Convert.ToInt64(arg);
            }
            catch (FormatException)
            {
            }
            catch (OverflowException)
            {
            }
            return chatId;
        }
        private static void GetCommand()
        {
            string command = ReadLine(_commandsLine);
            string[] commands = command.Split(new char[] { ' ' }, 2);
            try
            {
                switch (commands[0])
                {
                    case "gc":
                        _client.Send(new TdApi.GetChat(GetChatId(commands[1])), _defaultHandler);
                        break;
                    case "me":
                        _client.Send(new TdApi.GetMe(), _defaultHandler);
                        break;
                    case "sm":
                        string[] args = commands[1].Split(new char[] { ' ' }, 2);
                        sendMessage(GetChatId(args[0]), args[1]);
                        break;
                    case "lo":
                        _haveAuthorization = false;
                        _client.Send(new TdApi.LogOut(), _defaultHandler);
                        break;
                    case "r":
                        _haveAuthorization = false;
                        _client.Send(new TdApi.Close(), _defaultHandler);
                        break;
                    case "q":
                        _needQuit = true;
                        _haveAuthorization = false;
                        _client.Send(new TdApi.Close(), _defaultHandler);
                        break;
                    default:
                        Print("Unsupported command: " + command);
                        break;
                }
            }
            catch (IndexOutOfRangeException)
            {
                Print("Not enough arguments");
            }
        }
        public static TdApi.InputMessageSticker getSticker( string sticker_id, int sticker_width, int stikcer_height,
                                                            string thumbnail_id, int thumbnail_width, int thumbnail_height,
                                                            string emoji)
        {
            TdApi.InputFileRemote file = new TdApi.InputFileRemote(sticker_id);
            TdApi.InputThumbnail thumbnail = new TdApi.InputThumbnail(  new TdApi.InputFileRemote(thumbnail_id),
                                                                        thumbnail_width,
                                                                        thumbnail_height);
            TdApi.InputMessageSticker sticker = new TdApi.InputMessageSticker(  file, 
                                                                                thumbnail,
                                                                                sticker_width,
                                                                                stikcer_height,
                                                                                emoji);
            return sticker;

        }

        public string dialog_text = "";
        private static long chatId = 0;
        private static string user_nick = "";
        private static string user_nick_1 = "";
        private static string send_message = "";
        private static long replyId = 0;
        private static long userId = 0;
        public static bool can_send = true;

        public static string GetCurrenTime()
        {
            DateTime localDate = DateTime.Now;
            return localDate.ToString(new CultureInfo("ru-RU"));
        }
        public struct sendMessageInfo
        {
            public long chatId;
            public string user_nick_1;
            public string user_nick_2;
            public string send_message;
            public long replyId;
            public long userId_1;
            public long userId_2;
            public string sticker;
            public string eat;

            public bool can_change;

            public sendMessageInfo()
            {
                this.can_change = true;
            }
        }
        public static sendMessageInfo MESSAGE_INFO = new sendMessageInfo();

        private static TdApi.MessageContent _message_content_furr = null;
        private static TdApi.AuthorizationState _authorizationState = null;
        private static Td.Client _client = null;
        private static volatile bool _haveAuthorization = false;
        private static volatile AutoResetEvent _gotAuthorization = new AutoResetEvent(false);
        private static volatile bool _needQuit = false;
        private static readonly string _newLine = Environment.NewLine;
        private static readonly string _commandsLine = "Enter command (gc <chatId> - GetChat, me - GetMe, sm <chatId> <message> - SendMessage, lo - LogOut, r - Restart, q - Quit): ";
        private static volatile string _currentPrompt = null;
        private static volatile bool _canQuit = false;
        private readonly static Td.ClientResultHandler _defaultHandler = new DefaultHandler();

        private static Td.Client CreateTdClient()
        {
            return Td.Client.Create(new UpdateHandler());
        }

        private class UpdateHandler : Td.ClientResultHandler
        {
            int a = 0;
            void Td.ClientResultHandler.OnResult(TdApi.BaseObject @object)
            {
                if (@object is TdApi.UpdateAuthorizationState)
                {
                    OnAuthorizationStateUpdated((@object as TdApi.UpdateAuthorizationState).AuthorizationState);
                }
                else if (@object is TdApi.UpdateNewMessage)
                {
                    FurryChatHandler((@object as TdApi.UpdateNewMessage).Message);

                }
                else
                {
                    // Print("Unsupported update: " + @object);
                }
            }
        }

        private class AuthorizationRequestHandler : Td.ClientResultHandler
        {
            void Td.ClientResultHandler.OnResult(TdApi.BaseObject @object)
            {
                if (@object is TdApi.Error)
                {
                    Print("Receive an error:" + _newLine + @object);
                    OnAuthorizationStateUpdated(null); // repeat last action
                }
                else
                {
                    // result is already received through UpdateAuthorizationState, nothing to do
                }
            }
        }

        static bool furry_handler = false;
        static bool furry_handler_1 = false;
        static bool furry_handler_2 = false;
        static bool furry_handler_3 = false;
        static bool chat_info = false;
        private class DefaultHandler : Td.ClientResultHandler
        {
            void Td.ClientResultHandler.OnResult(TdApi.BaseObject @object)
            {
                //("New OBJECT!");
                if ((@object is TdApi.User) && furry_handler)
                {
                    MESSAGE_INFO.user_nick_1 = (@object as TdApi.User).FirstName + " " + (@object as TdApi.User).LastName;
                    MESSAGE_INFO.send_message = "🧱 || " + MESSAGE_INFO.user_nick_1 + MESSAGE_INFO.send_message;

                    TdApi.TextEntity ent_1 = new TdApi.TextEntity(6, MESSAGE_INFO.user_nick_1.Length, new TdApi.TextEntityTypeMentionName(MESSAGE_INFO.userId_1));
                    TdApi.TextEntity[] ent = new TdApi.TextEntity[1] { ent_1 };
                    TdApi.InputMessageContent in_mess = new TdApi.InputMessageText(new TdApi.FormattedText(MESSAGE_INFO.send_message, ent), false, true);
                    if (can_send)
                        sendReplyMessage(MESSAGE_INFO.chatId, MESSAGE_INFO.replyId, in_mess);
                    WriteInFileInfo("log.txt", MESSAGE_INFO);
                    furry_handler = false;
                }
                else if ((@object is TdApi.User) && furry_handler_1)
                {
                    //((@object is TdApi.User));
                    MESSAGE_INFO.user_nick_1 = (@object as TdApi.User).FirstName + " " + (@object as TdApi.User).LastName;
                    TdApi.TextEntity ent_1 = new TdApi.TextEntity(6, MESSAGE_INFO.user_nick_1.Length, new TdApi.TextEntityTypeMentionName(MESSAGE_INFO.userId_1));
                    TdApi.TextEntity ent_2 = new TdApi.TextEntity(6 + MESSAGE_INFO.user_nick_1.Length + MESSAGE_INFO.send_message.Length + 2, MESSAGE_INFO.user_nick_2.Length, new TdApi.TextEntityTypeMention());
                    TdApi.TextEntity[] ent = new TdApi.TextEntity[2] { ent_1, ent_2 };
                    string mes = "🧱 || " + MESSAGE_INFO.user_nick_1 + MESSAGE_INFO.send_message + '@' + MESSAGE_INFO.user_nick_2 + "! :3";
                    TdApi.InputMessageContent in_mess = new TdApi.InputMessageText(new TdApi.FormattedText(mes, ent), false, true);
                    if (can_send) 
                        sendReplyMessage(MESSAGE_INFO.chatId, MESSAGE_INFO.replyId, in_mess);
                    WriteInFileInfo("log.txt", MESSAGE_INFO);
                    furry_handler_1 = false;
                }
                else if (((@object is TdApi.Message) && furry_handler_2))
                {
                    //("Curr MESSAGE");
                    if ((@object as TdApi.Message).SenderId is TdApi.MessageSenderUser user)
                    {
                        //Console.WriteLine("USER: " + '\n' + user);
                        MESSAGE_INFO.userId_2 = user.UserId;
                        //("Curr chat id 1: " + MESSAGE_INFO.chatId);
                        _client.Send(new TdApi.GetUser(MESSAGE_INFO.userId_2), _defaultHandler);
                    }
                }
                else if (((@object is TdApi.User) && furry_handler_2))
                {
                    MESSAGE_INFO.user_nick_2 = (@object as TdApi.User).FirstName + " " + (@object as TdApi.User).LastName;
                    furry_handler_2 = false;
                    furry_handler_3 = true;
                    //("Curr chat id 2: " + MESSAGE_INFO.chatId);
                    _client.Send(new TdApi.GetUser(MESSAGE_INFO.userId_1), _defaultHandler);
                }
                else if (((@object is TdApi.User) && furry_handler_3))
                {
                    //("Curr chat id 3: " + MESSAGE_INFO.chatId);
                    MESSAGE_INFO.user_nick_1 = (@object as TdApi.User).FirstName + " " + (@object as TdApi.User).LastName;
                    TdApi.TextEntity ent_1 = new TdApi.TextEntity(6, MESSAGE_INFO.user_nick_1.Length, new TdApi.TextEntityTypeMentionName(MESSAGE_INFO.userId_1));
                    TdApi.TextEntity ent_2 = new TdApi.TextEntity(6 + MESSAGE_INFO.user_nick_1.Length + MESSAGE_INFO.send_message.Length, MESSAGE_INFO.user_nick_2.Length, new TdApi.TextEntityTypeMentionName(MESSAGE_INFO.userId_2));
                    TdApi.TextEntity[] ent = new TdApi.TextEntity[2] { ent_1, ent_2 };
                    string mes = "🧱 || " + MESSAGE_INFO.user_nick_1 + MESSAGE_INFO.send_message + MESSAGE_INFO.user_nick_2 + "! :3";
                    TdApi.InputMessageContent in_mess = new TdApi.InputMessageText(new TdApi.FormattedText(mes, ent), false, true);
                    if (can_send)
                    {
                        //Console.WriteLine("Here");
                        sendReplyMessage(MESSAGE_INFO.chatId, MESSAGE_INFO.replyId, in_mess);
                        MESSAGE_INFO.can_change = false;
                        if (MESSAGE_INFO.send_message == " лизнул ")
                            sendReplyMessage(MESSAGE_INFO.chatId, MESSAGE_INFO.replyId, getMessageSticker("👅"));
                        else if (MESSAGE_INFO.send_message == " шлёпнул ")
                            sendReplyMessage(MESSAGE_INFO.chatId, MESSAGE_INFO.replyId, getMessageSticker("🗞"));
                        else if (   MESSAGE_INFO.eat == "пицца" ||
                                    MESSAGE_INFO.eat == "мясо" ||
                                    MESSAGE_INFO.eat == "брецель" ||
                                    MESSAGE_INFO.eat == "eat")
                            sendReplyMessage(MESSAGE_INFO.chatId, MESSAGE_INFO.replyId, getMessageSticker(MESSAGE_INFO.eat));
                        else if (MESSAGE_INFO.send_message == " кусьнул ")
                            sendReplyMessage(MESSAGE_INFO.chatId, MESSAGE_INFO.replyId, getMessageSticker("кусь"));
                        else if (MESSAGE_INFO.send_message == " обнял ")
                            sendReplyMessage(MESSAGE_INFO.chatId, MESSAGE_INFO.replyId, getMessageSticker("обнять"));
                        else if (MESSAGE_INFO.send_message == " притянул к себе ")
                            sendReplyMessage(MESSAGE_INFO.chatId, MESSAGE_INFO.replyId, getMessageSticker("притянуть"));
                        else if (MESSAGE_INFO.send_message == " прижал к себе ")
                            sendReplyMessage(MESSAGE_INFO.chatId, MESSAGE_INFO.replyId, getMessageSticker("притянуть"));
                        else if (MESSAGE_INFO.eat == "чес")
                            sendReplyMessage(MESSAGE_INFO.chatId, MESSAGE_INFO.replyId, getMessageSticker("чес"));
                        else if (MESSAGE_INFO.eat == " погладил ")
                            sendReplyMessage(MESSAGE_INFO.chatId, MESSAGE_INFO.replyId, getMessageSticker("гладь"));
                        else
                            MESSAGE_INFO.can_change = true;
                        MESSAGE_INFO.can_change = true;
                    }
                    WriteInFileInfo("log.txt", MESSAGE_INFO);
                    furry_handler_3 = false;
                    MESSAGE_INFO.can_change = true;
                }
                if (@object is TdApi.ChatMessageSenders)
                {
                    //("HERE");
                    //((@object as TdApi.ChatMessageSenders));
                    chat_info = false;
                    MESSAGE_INFO.can_change = true;
                }
                //("INCOME: " + @object);
            }
        }
        public static void WriteInFileInfo(string file_path, sendMessageInfo info)
        {
            File.AppendAllText(file_path, "\nMessage info:\n");
            foreach (var field in typeof(sendMessageInfo).GetFields(BindingFlags.Instance |
                                                 BindingFlags.NonPublic |
                                                 BindingFlags.Public))
            {
                //if (field.GetValue(info) != null)
                    File.AppendAllText(file_path, field.Name + "\t\t = " + field.GetValue(info) + '\n');
            }
            File.AppendAllText(file_path, "\n\n");
        }

        public static TdApi.InputMessageSticker getMessageSticker(string emoji)
        {
            if (emoji == "👅")
                return getSticker("CAACAgIAAxkDAAEE5p5kgZ5zpS99G-AFrBlsDRnM83wq3wACcwADHZtLExHgQCfe1k6lLwQ", 512, 512,
                            "AAMCAgADGQMAAQTmnmSBnnOlL30b4AWsGWwNGczzfCrfAAJzAAMdm0sTEeBAJ97WTqUBAAdtAAMvBA", 320, 320,
                            emoji);
            else if (emoji == "пицца")
                return getSticker("CAACAgIAAxkDAAEE6G1kgfWQlR3H3NXnP6TB7HPw6Ac7ygAC6wADHZtLE7mvzPKCg8vuLwQ", 512, 512,
                            "AAMCAgADGQMAAQTobWSB9ZCVHcfc1ec_pMHsc_DoBzvKAALrAAMdm0sTua_M8oKDy-4BAAdtAAMvBA", 320, 320,
                            "🍕");
            else if (emoji == "мясо")
                return getSticker("CAACAgIAAxkDAAEE6GxkgfWPeuej_xcDVOm9cMuAzyiY0gAC3AADHZtLE1EZCtrmkcL1LwQ", 512, 512,
                            "AAMCAgADGQMAAQTobGSB9Y9656P_FwNU6b1wy4DPKJjSAALcAAMdm0sTURkK2uaRwvUBAAdtAAMvBA", 320, 320,
                            "🍴");
            else if (emoji == "eat" || emoji == "брецель")
                return getSticker("CAACAgIAAxkDAAEE6G5kgfWS28HteTtFMGUHNQbiYcZCiAAC7AADHZtLE0ES7y8Wt_-5LwQ", 512, 512,
                            "AAMCAgADGQMAAQTobmSB9ZLbwe15O0UwZQc1BuJhxkKIAALsAAMdm0sTQRLvLxa3_7kBAAdtAAMvBA", 320, 320,
                            "🥨");
            else if (emoji == "кусь")
                return getSticker("CAACAgIAAxkDAAEE6llkg1DyvblrwzJZYNVUf7Xzz2xrjAAC6gADHZtLEytyD2cI6rwfLwQ", 512, 512,
                            "AAMCAgADGQMAAQTqWWSDUPK9uWvDMllg1VR_tfPPbGuMAALqAAMdm0sTK3IPZwjqvB8BAAdtAAMvBA", 320, 320,
                            "🍴");
            else if (emoji == "обнять")
                return getSticker("CAACAgIAAxkDAAEE6VRkgyredR9PS2wtN1Ygg7_2vpMkhQACZwADHZtLE0guu71RwZ50LwQ", 512, 512,
                            "AAMCAgADGQMAAQTpVGSDKt51H09LbC03ViCDv_a-kySFAAJnAAMdm0sTSC67vVHBnnQBAAdtAAMvBA", 320, 320,
                            "🤗");
            else if (emoji == "притянуть")
                return getSticker("CAACAgIAAxkDAAEE6VVkgyr20rAmr1TVYZDkCNGTE7qdMwACdgADHZtLE_0jrZU0HasTLwQ", 512, 512,
                            "AAMCAgADGQMAAQTpVWSDKvbSsCavVNVhkOQI0ZMTup0zAAJ2AAMdm0sT_SOtlTQdqxMBAAdtAAMvBA", 320, 320,
                            "🤗");
            else if (emoji == "чес")
                return getSticker("CAACAgIAAxkDAAEE6Q5kgwHNkhy9uiUGiMepYGXHsEJffwAC-AADHZtLE8pEEoJTRqReLwQ", 512, 512,
                            "AAMCAgADGQMAAQTpDmSDAc2SHL26JQaIx6lgZcewQl9_AAL4AAMdm0sTykQSglNGpF4BAAdtAAMvBA", 320, 320,
                            "🐶");
            else if (emoji == "гладь")
                return getSticker("CAACAgEAAxkDAAEE6l1kg1HLSueElI_kSPk5_NMzi9IENgACLwIAAvtv6UaqGOgUT3l5dC8E", 512, 512,
                            "AAMCAQADGQMAAQTqXWSDUctK54SUj-RI-Tn80zOL0gQ2AAIvAgAC-2_pRqoY6BRPeXl0AQAHbQADLwQ", 320, 320,
                            "🐶");
            else
                return getSticker("CAACAgEAAxkDAAEE5qhkgaLdFsMUUtnYLjD7niCZiYUr7gACQQMAAguW6UbaMxVPx3R3pS8E", 512, 512,
                            "AAMCAQADGQMAAQTmqGSBot0WwxRS2dguMPueIJmJhSvuAAJBAwACC5bpRtozFU_HdHelAQAHbQADLwQ", 320, 320,
                            emoji);
        }

        public struct XOmap
        {
            string[] map = new string[9] {  "💢", "💢", "💢",
                                            "💢", "💢", "💢",
                                            "💢", "💢", "💢"   };
            bool game_begun = false;
            long gamer_id_1 = 0;
            long gamer_id_2 = 0;
            
            public XOmap()
            {
            }
        }

        static long userId_1 = 0;
        static string userName_2 = "";
        static string search = "";
        private static void FurryChatHandler(TdApi.Message message)
        {
            _message_content_furr = message.Content;
            //if (_message_content_furr is TdApi.MessageSticker messl)
            //{
            //    if (message.SenderId is TdApi.MessageSenderUser usr)
            //    {
            //        if (usr.UserId == 827991987)
            //        {
            //            try
            //            {
            //                Console.WriteLine("Sticker id: " + messl.Sticker.StickerValue.Remote.Id);
            //                Console.WriteLine("Thumbnail id: " + messl.Sticker.Thumbnail.File.Remote.Id);
            //            }
            //            catch (System.NullReferenceException e)
            //            {
            //                Console.WriteLine("Meh");
            //            }
            //        }
            //    }
            //}
            if (_message_content_furr is TdApi.MessageText mess && MESSAGE_INFO.can_change)
            {

                chatId = message.ChatId;
                if (chatId != 0)
                {
                    ////(mess.Text.Text);
                    string[] mess_comm = mess.Text.Text.Split(new char[] { ' ' });
                    if (mess_comm.Length != 0)
                    {
                        if (mess_comm[0] == "Фыр")
                        {
                            if (message.SenderId is TdApi.MessageSenderUser user)
                            {
                                File.AppendAllText("log.txt", "\n\n" + GetCurrenTime());
                                File.AppendAllText("log.txt", "\n==================\n");
                                File.AppendAllText("log.txt", "Команда: " + mess_comm[0] + '\n');
                                MESSAGE_INFO.can_change = false;
                                //("===========\nMessage come: \nchat id: " + message.ChatId + '\n' + "text: " + mess.Text + "\nuserID: " + user.UserId);
                                MESSAGE_INFO.send_message = " фыркнул!";
                                MESSAGE_INFO.chatId = message.ChatId;
                                MESSAGE_INFO.replyId = message.Id;
                                MESSAGE_INFO.userId_1 = user.UserId;
                                furry_handler = true;
                                _client.Send(new TdApi.GetUser(MESSAGE_INFO.userId_1), _defaultHandler);
                            }
                        }
                        else if (mess_comm.Length >= 1)
                        {
                            if (mess_comm[0].ToLower() == "кусь" || mess_comm[0].ToLower() == "укусить")
                            {
                                if (message.ReplyToMessageId != 0)
                                {
                                    if (message.SenderId is TdApi.MessageSenderUser user)
                                    {
                                        File.AppendAllText("log.txt", "\n\n" + GetCurrenTime());
                                        File.AppendAllText("log.txt", "\n==================\n");
                                        File.AppendAllText("log.txt", "Команда: " + mess_comm[0] + '\n');
                                        MESSAGE_INFO.can_change = false;
                                        //("===========\nMessage come: \nchat id: " + message.ChatId + '\n' + "text: " + mess.Text);
                                        MESSAGE_INFO.send_message = " кусьнул ";
                                        MESSAGE_INFO.chatId = message.ChatId;
                                        MESSAGE_INFO.replyId = message.ReplyToMessageId;
                                        MESSAGE_INFO.userId_1 = user.UserId;
                                        furry_handler_2 = true;
                                        _client.Send(new TdApi.GetMessage(MESSAGE_INFO.chatId, MESSAGE_INFO.replyId), _defaultHandler);
                                    }
                                }
                            }
                            else if (mess_comm[0].ToLower() == "лизь" || mess_comm[0].ToLower() == "лизнуть")
                            {
                                if (message.ReplyToMessageId != 0)
                                {
                                    if (message.SenderId is TdApi.MessageSenderUser user)
                                    {
                                        File.AppendAllText("log.txt", "\n\n" + GetCurrenTime());
                                        File.AppendAllText("log.txt", "\n==================\n");
                                        File.AppendAllText("log.txt", "Команда: " + mess_comm[0] + '\n');
                                        MESSAGE_INFO.can_change = false;
                                        //("===========\nMessage come: \nchat id: " + message.ChatId + '\n' + "text: " + mess.Text);
                                        MESSAGE_INFO.send_message = " лизнул ";
                                        MESSAGE_INFO.chatId = message.ChatId;
                                        MESSAGE_INFO.replyId = message.ReplyToMessageId;
                                        MESSAGE_INFO.userId_1 = user.UserId;
                                        furry_handler_2 = true;
                                        _client.Send(new TdApi.GetMessage(MESSAGE_INFO.chatId, MESSAGE_INFO.replyId), _defaultHandler);
                                    }
                                }
                            }

                            else if (mess_comm[0].ToLower() == "притянуть")
                            {
                                if (message.ReplyToMessageId != 0)
                                {
                                    if (message.SenderId is TdApi.MessageSenderUser user)
                                    {
                                        File.AppendAllText("log.txt", "\n\n" + GetCurrenTime());
                                        File.AppendAllText("log.txt", "\n==================\n");
                                        File.AppendAllText("log.txt", "Команда: " + mess_comm[0] + '\n');
                                        MESSAGE_INFO.can_change = false;
                                        //("===========\nMessage come: \nchat id: " + message.ChatId + '\n' + "text: " + mess.Text);
                                        MESSAGE_INFO.send_message = " притянул к себе ";
                                        MESSAGE_INFO.chatId = message.ChatId;
                                        MESSAGE_INFO.replyId = message.ReplyToMessageId;
                                        MESSAGE_INFO.userId_1 = user.UserId;
                                        furry_handler_2 = true;
                                        _client.Send(new TdApi.GetMessage(MESSAGE_INFO.chatId, MESSAGE_INFO.replyId), _defaultHandler);
                                    }
                                }
                            }
                            else if (mess_comm[0].ToLower() == "прижать")
                            {
                                if (message.ReplyToMessageId != 0)
                                {
                                    if (message.SenderId is TdApi.MessageSenderUser user)
                                    {
                                        File.AppendAllText("log.txt", "\n\n" + GetCurrenTime());
                                        File.AppendAllText("log.txt", "\n==================\n");
                                        File.AppendAllText("log.txt", "Команда: " + mess_comm[0] + '\n');
                                        MESSAGE_INFO.can_change = false;
                                        //("===========\nMessage come: \nchat id: " + message.ChatId + '\n' + "text: " + mess.Text);
                                        MESSAGE_INFO.send_message = " прижал к себе ";
                                        MESSAGE_INFO.chatId = message.ChatId;
                                        MESSAGE_INFO.replyId = message.ReplyToMessageId;
                                        MESSAGE_INFO.userId_1 = user.UserId;
                                        furry_handler_2 = true;
                                        _client.Send(new TdApi.GetMessage(MESSAGE_INFO.chatId, MESSAGE_INFO.replyId), _defaultHandler);
                                    }
                                }
                            }
                            else if (mess_comm[0].ToLower() == "обнять")
                            {
                                if (message.ReplyToMessageId != 0)
                                {
                                    if (message.SenderId is TdApi.MessageSenderUser user)
                                    {
                                        File.AppendAllText("log.txt", "\n\n" + GetCurrenTime());
                                        File.AppendAllText("log.txt", "\n==================\n");
                                        File.AppendAllText("log.txt", "Команда: " + mess_comm[0] + '\n');
                                        MESSAGE_INFO.can_change = false;
                                        //("===========\nMessage come: \nchat id: " + message.ChatId + '\n' + "text: " + mess.Text);
                                        MESSAGE_INFO.send_message = " обнял ";
                                        MESSAGE_INFO.chatId = message.ChatId;
                                        MESSAGE_INFO.replyId = message.ReplyToMessageId;
                                        MESSAGE_INFO.userId_1 = user.UserId;
                                        furry_handler_2 = true;
                                        _client.Send(new TdApi.GetMessage(MESSAGE_INFO.chatId, MESSAGE_INFO.replyId), _defaultHandler);
                                    }
                                }
                            }
                            else if (mess_comm[0].ToLower() == "погладить")
                            {
                                if (message.ReplyToMessageId != 0)
                                {
                                    if (message.SenderId is TdApi.MessageSenderUser user)
                                    {
                                        File.AppendAllText("log.txt", "\n\n" + GetCurrenTime());
                                        File.AppendAllText("log.txt", "\n==================\n");
                                        File.AppendAllText("log.txt", "Команда: " + mess_comm[0] + '\n');
                                        MESSAGE_INFO.can_change = false;
                                        //("===========\nMessage come: \nchat id: " + message.ChatId + '\n' + "text: " + mess.Text);
                                        MESSAGE_INFO.send_message = " погладил ";
                                        MESSAGE_INFO.chatId = message.ChatId;
                                        MESSAGE_INFO.replyId = message.ReplyToMessageId;
                                        MESSAGE_INFO.userId_1 = user.UserId;
                                        furry_handler_2 = true;
                                        _client.Send(new TdApi.GetMessage(MESSAGE_INFO.chatId, MESSAGE_INFO.replyId), _defaultHandler);
                                    }
                                }
                            }
                            else if (mess_comm[0].ToLower() == "шлепнуть" || mess_comm[0].ToLower() == "шлёпнуть")
                            {
                                if (message.ReplyToMessageId != 0)
                                {
                                    if (message.SenderId is TdApi.MessageSenderUser user)
                                    {
                                        File.AppendAllText("log.txt", "\n\n" + GetCurrenTime());
                                        File.AppendAllText("log.txt", "\n==================\n");
                                        File.AppendAllText("log.txt", "Команда: " + mess_comm[0] + '\n');
                                        MESSAGE_INFO.can_change = false;
                                        //("===========\nMessage come: \nchat id: " + message.ChatId + '\n' + "text: " + mess.Text);
                                        MESSAGE_INFO.send_message = " шлёпнул ";
                                        MESSAGE_INFO.chatId = message.ChatId;
                                        MESSAGE_INFO.replyId = message.ReplyToMessageId;
                                        MESSAGE_INFO.userId_1 = user.UserId;
                                        furry_handler_2 = true;
                                        _client.Send(new TdApi.GetMessage(MESSAGE_INFO.chatId, MESSAGE_INFO.replyId), _defaultHandler);
                                    }
                                }
                            }
                            else if (mess_comm[0].ToLower() == "почесать")
                            {
                                if (message.ReplyToMessageId != 0)
                                {
                                    if (message.SenderId is TdApi.MessageSenderUser user)
                                    {
                                        File.AppendAllText("log.txt", "\n\n" + GetCurrenTime());
                                        File.AppendAllText("log.txt", "\n==================\n");
                                        File.AppendAllText("log.txt", "Команда: " + mess_comm[0] + '\n');
                                        MESSAGE_INFO.can_change = false;
                                        //("===========\nMessage come: \nchat id: " + message.ChatId + '\n' + "text: " + mess.Text);
                                        MESSAGE_INFO.send_message = " почесал " + GetRandomWord("почесать") + " ";
                                        MESSAGE_INFO.eat = "чес";
                                        MESSAGE_INFO.chatId = message.ChatId;
                                        //("New chat id: " + chatId);
                                        MESSAGE_INFO.replyId = message.ReplyToMessageId;
                                        MESSAGE_INFO.userId_1 = user.UserId;
                                        furry_handler_2 = true;
                                        _client.Send(new TdApi.GetMessage(MESSAGE_INFO.chatId, MESSAGE_INFO.replyId), _defaultHandler);
                                    }
                                }
                            }
                            else if (mess_comm[0].ToLower() == "покормить")
                            {
                                if (message.ReplyToMessageId != 0)
                                {
                                    if (message.SenderId is TdApi.MessageSenderUser user)
                                    {
                                        MESSAGE_INFO.can_change = false;
                                        File.AppendAllText("log.txt", "\n\n" + GetCurrenTime());
                                        File.AppendAllText("log.txt", "\n==================\n");
                                        File.AppendAllText("log.txt", "Команда: " + mess_comm[0] + '\n');
                                        if (mess_comm.Length > 1)
                                        {
                                            if (mess_comm[1].ToLower() == "пиццей" ||
                                                mess_comm[1].ToLower() == "пицца")
                                                MESSAGE_INFO.eat = "пицца";
                                            else if (mess_comm[1].ToLower() == "мясо" ||
                                                        mess_comm[1].ToLower() == "мясом" ||
                                                        mess_comm[1].ToLower() == "курицей" ||
                                                        mess_comm[1].ToLower() == "курица" ||
                                                        mess_comm[1].ToLower() == "хамон" ||
                                                        mess_comm[1].ToLower() == "хамоном")
                                                MESSAGE_INFO.eat = "мясо";
                                            else if (mess_comm[1].ToLower() == "брецель" ||
                                                        mess_comm[1].ToLower() == "брецелем" ||
                                                        mess_comm[1].ToLower() == "пряник" ||
                                                        mess_comm[1].ToLower() == "пряником" ||
                                                        mess_comm[1].ToLower() == "булкой" ||
                                                        mess_comm[1].ToLower() == "булка" ||
                                                        mess_comm[1].ToLower() == "булочка" ||
                                                        mess_comm[1].ToLower() == "булочкой" ||
                                                        mess_comm[1].ToLower() == "хлеб" ||
                                                        mess_comm[1].ToLower() == "хлебом")
                                                MESSAGE_INFO.eat = "брецель";
                                            else
                                            {
                                                MESSAGE_INFO.eat = "eat";
                                            }
                                            MESSAGE_INFO.send_message = " покормил ";
                                        }
                                        else
                                        {
                                            MESSAGE_INFO.eat = "eat";
                                            MESSAGE_INFO.send_message = " покормил " + GetRandomWord(mess_comm[0]) + " ";
                                        }
                                        MESSAGE_INFO.chatId = message.ChatId;
                                        MESSAGE_INFO.replyId = message.ReplyToMessageId;
                                        MESSAGE_INFO.userId_1 = user.UserId;
                                        furry_handler_2 = true;
                                        _client.Send(new TdApi.GetMessage(MESSAGE_INFO.chatId, MESSAGE_INFO.replyId), _defaultHandler);

                                    }
                                }

                            }
                            else if (mess_comm[0] == "Погода" || mess_comm[0] == "погода")
                            {
                                //("===========\nMessage come: \nchat id: " + message.ChatId + '\n' + "text: " + mess.Text);
                                if (message.SenderId is TdApi.MessageSenderUser user)
                                {
                                    File.AppendAllText("log.txt", "\n\n" + GetCurrenTime());
                                    File.AppendAllText("log.txt", "\n==================\n");
                                    File.AppendAllText("log.txt", "Команда: " + mess_comm[0] + '\n');
                                    if (mess_comm.Length > 1)
                                    {
                                        string weather_check = "";
                                        for (int i = 1; i <= mess_comm.Length - 1; i++)
                                        {
                                            weather_check += mess_comm[i];
                                        }
                                        Weather_Root weather = get_weather(weather_check);
                                        if (weather != null)
                                        {
                                            MESSAGE_INFO.can_change = false;
                                            int weather_code = weather.current_weather.weathercode;
                                            weatherInfo info = WeatherUnicode(weather_code);
                                            MESSAGE_INFO.send_message = info.weatherSmile + MESSAGE_INFO.send_message;
                                            MESSAGE_INFO.send_message += "Состояние: " + info.definition + '\n';
                                            MESSAGE_INFO.send_message += "Температура: " + SetSignToTemp(weather.current_weather.temperature) + " ℃" + '\n';
                                            MESSAGE_INFO.send_message += "Скорость ветра: " + '\t' + weather.current_weather.windspeed + " м/с" + '\n';
                                            double wind_dir = weather.current_weather.winddirection;
                                            MESSAGE_INFO.send_message += "Направление ветра: " + '\t' + GradToDirection(wind_dir) + '\n';
                                            MESSAGE_INFO.send_message += "LouieAusserhalbService 🦊";

                                        }
                                        else
                                        {
                                            MESSAGE_INFO.send_message = "Нет такого города!";
                                        }
                                        MESSAGE_INFO.chatId = message.ChatId;
                                        MESSAGE_INFO.replyId = message.Id;
                                        MESSAGE_INFO.userId_1 = user.UserId;
                                        if (can_send)
                                            sendReplyMessage(MESSAGE_INFO.chatId, MESSAGE_INFO.replyId, MESSAGE_INFO.send_message);
                                        WriteInFileInfo("log.txt", MESSAGE_INFO);
                                    }
                                }
                            }
                            else if (mess_comm.Length >= 3)
                            {
                                if ((((mess_comm[0] + mess_comm[1]).ToLower() == "чтотакое") ||
                                ((mess_comm[0] + mess_comm[1]).ToLower() == "ктотакой") ||
                                ((mess_comm[0] + mess_comm[1]).ToLower() == "ктотакая") ||
                                ((mess_comm[0] + mess_comm[1]).ToLower() == "ктотакие")) && message.ChatId != -1001476181121)
                                {
                                    File.AppendAllText("log.txt", "\n\n" + GetCurrenTime());
                                    File.AppendAllText("log.txt", "\n==================\n");
                                    File.AppendAllText("log.txt", "Команда: " + mess_comm[0] + mess_comm[1] + '\n');

                                    MESSAGE_INFO.can_change = false;
                                    //("===========\nMessage come: \nchat id: " + message.ChatId + '\n' + "text: " + mess.Text);
                                    MESSAGE_INFO.chatId = message.ChatId;
                                    MESSAGE_INFO.replyId = message.Id;
                                    search = "";
                                    for (int k = 2; k < mess_comm.Length - 2; k++)
                                        search += mess_comm[k] + " ";
                                    search += mess_comm[mess_comm.Length - 1];
                                    File.AppendAllText("log.txt", "Запрос: " + search + '\n');
                                    //(search);
                                    
                                    if (!CheckObscence(search))
                                    {
                                        sendReplyMessage(MESSAGE_INFO.chatId, MESSAGE_INFO.replyId, "Обработка запроса, ждите 🔍");
                                        MESSAGE_INFO.can_change = false;
                                        string res = getWord(search);
                                        if (!CheckObscence(res))
                                        {
                                            File.AppendAllText("log.txt", "Отправление:\n" + res + '\n');
                                            if (/*chatId != -1001476181121 &&*/ can_send)
                                            {
                                                sendReplyMessage(MESSAGE_INFO.chatId, MESSAGE_INFO.replyId, res);
                                            }
                                            else
                                            {
                                                sendReplyMessage(MESSAGE_INFO.chatId, MESSAGE_INFO.replyId, "К сожалению, в этом чате этот запрос запрещён. Напишите в личные сообщения для обработки запроса" +
                                                                                                            '\n' + "LouieAusserhalbService 🦊");
                                                MESSAGE_INFO.can_change = true;
                                            }
                                        }
                                        else
                                        {
                                            sendReplyMessage(MESSAGE_INFO.chatId, MESSAGE_INFO.replyId, "Запрос отклонён: найденная статья, возможно, обладает нецензурной лексикой!" + '\n' + "LouieAusserhalbService 🦊");
                                            MESSAGE_INFO.can_change = true;
                                        }
                                    }
                                    else
                                    {
                                        sendReplyMessage(MESSAGE_INFO.chatId, MESSAGE_INFO.replyId, "Запрос отклонён: не ругайся здесь!" + '\n' + "LouieAusserhalbService 🦊");
                                    }
                                }
                                else if (mess_comm[0] + mess_comm[1] == "Почесатьпузико")
                                {
                                    if (message.SenderId is TdApi.MessageSenderUser user)
                                    {

                                        MESSAGE_INFO.can_change = false;
                                        //("===========\nMessage come: \nchat id: " + message.ChatId + '\n' + "text: " + mess.Text);
                                        MESSAGE_INFO.send_message = " почесал пузико ";
                                        MESSAGE_INFO.chatId = message.ChatId;
                                        MESSAGE_INFO.replyId = message.Id;
                                        MESSAGE_INFO.userId_1 = user.UserId;
                                        if (mess_comm[2][0] == '@')
                                        {
                                            MESSAGE_INFO.user_nick_2 = mess_comm[2].Trim(new char[] { '@' });
                                            furry_handler_1 = true;
                                            _client.Send(new TdApi.GetUser(MESSAGE_INFO.userId_1), _defaultHandler);
                                        }
                                    }
                                }
                            }
                            if (mess_comm.Length == 4)
                            {
                                if (mess_comm[0] + mess_comm[1] + mess_comm[2] == "Погладитьзаушком")
                                {
                                    if (message.SenderId is TdApi.MessageSenderUser user)
                                    {
                                        MESSAGE_INFO.can_change = false;
                                        //("===========\nMessage come: \nchat id: " + message.ChatId + '\n' + "text: " + mess.Text);
                                        MESSAGE_INFO.send_message = " погладил за ушком ";
                                        MESSAGE_INFO.chatId = message.ChatId;
                                        MESSAGE_INFO.replyId = message.Id;
                                        MESSAGE_INFO.userId_1 = user.UserId;
                                        if (mess_comm[3][0] == '@')
                                        {
                                            MESSAGE_INFO.user_nick_1 = mess_comm[3].Trim(new char[] { '@' });
                                            furry_handler_1 = true;
                                            _client.Send(new TdApi.GetUser(MESSAGE_INFO.userId_1), _defaultHandler);
                                        }
                                    }
                                }
                            }

                            if (mess_comm.Length == 2)
                            {
                                if (mess_comm[0] + mess_comm[1] == "Почесатьпузико")
                                {
                                    if (message.ReplyToMessageId != 0)
                                    {
                                        if (message.SenderId is TdApi.MessageSenderUser user)
                                        {
                                            File.AppendAllText("log.txt", "\n\n" + GetCurrenTime());
                                            File.AppendAllText("log.txt", "\n==================\n");
                                            File.AppendAllText("log.txt", "Команда: " + mess_comm[0] + mess_comm[1] + '\n');
                                            MESSAGE_INFO.can_change = false;
                                            //("===========\nMessage come: \nchat id: " + message.ChatId + '\n' + "text: " + mess.Text);
                                            MESSAGE_INFO.send_message = " почесал пузико ";
                                            MESSAGE_INFO.chatId = message.ChatId;
                                            MESSAGE_INFO.replyId = message.Id;
                                            MESSAGE_INFO.userId_1 = user.UserId;
                                            furry_handler_2 = true;
                                            _client.Send(new TdApi.GetMessage(MESSAGE_INFO.chatId, MESSAGE_INFO.replyId), _defaultHandler);
                                        }
                                    }
                                }
                                else if (mess_comm[0] + mess_comm[1] == "Кусьхвостик")
                                {
                                    if (message.ReplyToMessageId != 0)
                                    {
                                        if (message.SenderId is TdApi.MessageSenderUser user)
                                        {
                                            File.AppendAllText("log.txt", "\n\n" + GetCurrenTime());
                                            File.AppendAllText("log.txt", "\n==================\n");
                                            File.AppendAllText("log.txt", "Команда: " + mess_comm[0] + mess_comm[1] + '\n');
                                            MESSAGE_INFO.can_change = false;
                                            //("===========\nMessage come: \nchat id: " + message.ChatId + '\n' + "text: " + mess.Text);
                                            MESSAGE_INFO.send_message = " укусил за хвостик ";
                                            MESSAGE_INFO.chatId = message.ChatId;
                                            MESSAGE_INFO.replyId = message.ReplyInChatId;
                                            MESSAGE_INFO.userId_1 = user.UserId;
                                            furry_handler_2 = true;
                                            _client.Send(new TdApi.GetMessage(MESSAGE_INFO.chatId, MESSAGE_INFO.replyId), _defaultHandler);
                                        }
                                    }
                                }
                                
                                else if (mess_comm[0] == "Чат")
                                {
                                    if (message.SenderId is TdApi.MessageSenderUser user)
                                    {
                                        MESSAGE_INFO.can_change = false;
                                        MESSAGE_INFO.chatId = message.ChatId;
                                        MESSAGE_INFO.replyId = message.Id;
                                        chat_info = true;
                                        //("Here parse");
                                        long i;
                                        bool success = long.TryParse(mess_comm[1], out i);
                                        if (success)
                                        {
                                            //("Info Sending: " + i);
                                            //_client.Send(new TdApi.GetChatMember(i), _defaultHandler);
                                        }

                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        public static bool CheckObscence (string text)
        {
            string check_text = text;
            check_text = check_text.ToLower();
            //check_text = check_text.Replace(" ", "");
            check_text = check_text.Replace("\r", " ");
            check_text = check_text.Replace("\t", " ");
            check_text = check_text.Replace("\v", " ");
            check_text = check_text.Replace(",", " ");
            check_text = check_text.Replace(".", " ");
            check_text = check_text.Replace("«", " ");
            check_text = check_text.Replace("»", " ");
            check_text = check_text.Replace("!", " ");
            check_text = check_text.Replace("?", " ");
            check_text = check_text.Replace(":", " ");
            check_text = check_text.Replace(";", " ");
            check_text = check_text.Replace("/", " ");
            check_text = check_text.Replace("(", " ");
            check_text = check_text.Replace(")", " ");
            check_text = check_text.Replace("—", " ");

            check_text = check_text.Replace("а́", "а");
            check_text = check_text.Replace("е́", "е");
            check_text = check_text.Replace("и́", "и");
            check_text = check_text.Replace("о́", "о");
            check_text = check_text.Replace("у́", "у");
            check_text = check_text.Replace("ы́", "ы");
            check_text = check_text.Replace("э́", "э");
            check_text = check_text.Replace("ю́", "ю");
            check_text = check_text.Replace("я́", "я");

            bool es_gibt = false;
            StreamReader f = new StreamReader("mat.txt");
            while (!f.EndOfStream)
            {
                string s = f.ReadLine();
                if (check_text.Contains(s))
                {
                    es_gibt = true;
                    break;
                }
                
            }
            f.Close();
            return es_gibt;
        }

        public string[] obscence_words = new string[] { "бля", "блять", "" };

        static Random rnd = new Random();
        public static string GetRandomWord(string word)
        {
            string[] fruits = new string[] { "NICHTS" };
            if (word == "почесать")
                fruits = new string[] { "ушко", "животик", "головку", "спинку", "ножку", "ручку", "плечико", "затылок", "локоток", "носик" };
            else if (word == "покормить")
                fruits = new string[] { "арбузиком 🍉 ", "дынькой 🍈 ", "яблочком 🍎 ", "шоколадкой 🍫 ",
                                        "клубничкой 🍓 ", "пиццей 🍕 ", "бургером 🍔 ", "супом 🍲 ", "тортиком 🍰 ", "печенькой 🍪 " };
            return fruits[rnd.Next(0, fruits.Length)];
        }
        //public static string getWord(string search_info)
        //{
        //    string ser = GetHttpRequest("https://api.dictionaryapi.dev/api/v2/entries/en/Air");
        //    return ser;
        //}
        public static string getWord(string search_info)
        {
            
            try
            {
                var wiki = new Wiki("LouieAusService", "https://ru.wikipedia.org");
                var result = from s in wiki.Query.search(search_info)
                             select new { s.title, snippet = s.snippet.Substring(0, 30) };
                var arr = result.ToEnumerable().Take(1).ToArray();
                string res;
                string txt = "";
                if (arr.Length != 0)
                {
                    res = arr[0].title;
                }
                else
                {
                    txt = "По вашему запросу ничего не найдено" + '\n' + "LouieAusserhalbService 🦊";
                    return txt;
                }
                    var titles = wiki.CreateTitlesSource(res);
                    var pages =
                        titles.Select(
                            page => new
                            {
                                Title = page.info.title,
                                Text = page.revisions()
                                           .Where(r => r.section == "0" && r.parse)
                                           .Select(r => r.value)
                                           .FirstOrDefault(),
                                LangLinks = page.langlinks().ToEnumerable()
                            }).ToEnumerable();
                    var array = pages.ToArray();
                    foreach (var s_txt in array)
                        txt += s_txt;
                    //Console.WriteLine(txt);
                    if (txt == "")
                        txt = "По вашему запросу ничего не найдено" + '\n' + "LouieAusserhalbService 🦊";
                    return CutWikiText(HtmlToPlainText(txt));
                
            }
            catch (Exception e)
            {
                return "Ошибка: введите запрос еще раз (ну бывает :) ) \n" + "Вызвано исключение";
            }
            //("TEXT:", txt);
            
        }

        private static string CutWikiText(string text)
        {
            //(text + '\n');
            int tere_index = text.IndexOf('—');
            int indes = text.LastIndexOf(", LangLinks");
            if (indes != -1)
            {
                text = text.Substring(0, indes);
            }
            text += "\n↑";
            while (true)
            {
                tere_index = text.IndexOf('—');
                if (tere_index == -1)
                    break;
                //(text);
                string sub_str = text.Substring(tere_index);
                int index_new_str = sub_str.IndexOf('\n');
                if (index_new_str != -1)
                {
                    string new_str = sub_str.Substring(0, index_new_str);
                    if (new_str[index_new_str - 1] == '.')
                    {
                        break;
                    }
                    else
                    {
                        text = text.Substring(index_new_str + 1, text.Length - 1 - (index_new_str + 1));
                    }
                }
                else
                {
                    break;
                }
            }
            int last_before_tere_end_index = text.Substring(0, tere_index).LastIndexOf('\n');
            string res;
            if (last_before_tere_end_index != -1)
            {
                res = text.Substring(last_before_tere_end_index, text.Length - last_before_tere_end_index);

                text = res;
            }
            else
                res = text;

            while (true)
            {
                int k = res.IndexOf('↑');
                if (k == -1)
                    break;
                res = res.Substring(0, k);
            }
            while (true)
            {
                int k = res.IndexOf('←');
                if (k == -1)
                    break;
                res = res.Substring(0, k);
            }
            res = Regex.Replace(res, "\n\n", "");
            res = Regex.Replace(res, "\r", "");
            if (res.Length >= 700)
            {
                res = res.Substring(0, 650);
                res += "...";
            }
            for (int i = 0; i <= 50; i++)
            {
                string cifra = "[" + i + "]";
                res.Replace(cifra, "");
            }

            res += '\n' + "LouieAusserhalbService 🦊";
            return res;
        }
        private static string HtmlToPlainText(string html)
        {
            const string tagWhiteSpace = @"(>|$)+<";//matches one or more (white space or line breaks) between '>' and '<'
            const string stripFormatting = @"<[^>]*(>|$)";//match any character between '<' and '>', even when end tag is missing
            const string lineBreak = @"<(br|BR)\s{0,1}\/{0,1}>";//matches: <br>,<br/>,<br />,<BR>,<BR/>,<BR />
            var lineBreakRegex = new Regex(lineBreak, RegexOptions.Multiline);
            var stripFormattingRegex = new Regex(stripFormatting, RegexOptions.Multiline);
            var tagWhiteSpaceRegex = new Regex(tagWhiteSpace, RegexOptions.Multiline);

            var text = html;
            //Decode html specific characters
            text = System.Net.WebUtility.HtmlDecode(text);
            //Remove tag whitespace/line breaks
            text = tagWhiteSpaceRegex.Replace(text, "><");
            //Replace <br /> with line breaks
            text = lineBreakRegex.Replace(text, Environment.NewLine);
            //Strip formatting
            text = stripFormattingRegex.Replace(text, string.Empty);

            return text;
        }
        public static string StripTagsCharArray(string source)
        {
            char[] array = new char[source.Length];
            int arrayIndex = 0;
            bool inside = false;

            for (int i = 0; i < source.Length; i++)
            {
                char let = source[i];
                if (let == '<')
                {
                    inside = true;
                    continue;
                }
                if (let == '>')
                {
                    inside = false;
                    continue;
                }
                if (!inside)
                {
                    array[arrayIndex] = let;
                    arrayIndex++;
                }
            }
            return new string(array, 0, arrayIndex);
        }

        public static string HTMLToText(string HTMLCode)
        {
            // Remove new lines since they are not visible in HTML  
            //HTMLCode = HTMLCode.Replace("\n", " ");
            // Remove tab spaces  
            //HTMLCode = HTMLCode.Replace("\t", " ");
            // Remove multiple white spaces from HTML  
            HTMLCode = Regex.Replace(HTMLCode, "\\s+", " ");
            // Remove HEAD tag  
            HTMLCode = Regex.Replace(HTMLCode, "<head.*?</head>", ""
                                , RegexOptions.IgnoreCase | RegexOptions.Singleline);
            // Remove any JavaScript  
            HTMLCode = Regex.Replace(HTMLCode, "<script.*?</script>", ""
              , RegexOptions.IgnoreCase | RegexOptions.Singleline);
            // Replace special characters like &, <, >, " etc.  
            StringBuilder sbHTML = new StringBuilder(HTMLCode);
            // Note: There are many more special characters, these are just  
            // most common. You can add new characters in this arrays if needed  
            string[] OldWords = {"&nbsp;", "&amp;", "&quot;", "&lt;",
        "&gt;", "&reg;", "&copy;", "&bull;", "&trade;","&#39;"};
            string[] NewWords = { " ", "&", "\"", "<", ">", "Â®", "Â©", "â€¢", "â„¢", "\'" };
            for (int i = 0; i < OldWords.Length; i++)
            {
                sbHTML.Replace(OldWords[i], NewWords[i]);
            }
            // Check if there are line breaks (<br>) or paragraph (<p>)  
            sbHTML.Replace("<br>", "\n<br>");
            sbHTML.Replace("<br ", "\n<br ");
            sbHTML.Replace("<p ", "\n<p ");
            // Finally, remove all HTML tags and return plain text  
            return System.Text.RegularExpressions.Regex.Replace(
              sbHTML.ToString(), "<[^>]*>", "");
        }

        private static void Write<T>(IEnumerable<T> results)
        {
            var array = results.ToArray();

            //foreach (var result in array)
                //(result);

            //("Total: {0}", array.Length);
        }
        private static void Write<T>(WikiQueryPageResult<PageResult<T>> source)
        {
            Write(source.ToEnumerable());
        }

        private static void Write<T>(IEnumerable<PageResult<T>> source)
        {
            foreach (var page in source.Take(10))
            {
                //(page.Info.title);
                //foreach (var item in page.Data.Take(10))
                    //("  " + item);
            }
        }

        private static void Write<TSource, TResult>(WikiQueryResult<TSource, TResult> results)
        {
            Write(results.ToEnumerable().Take(10));
        }

        private static void PageResultProps(PagesSource<Page> pages)
        {
            var source = pages
                .Select(
                    p =>
                    PageResult.Create(
                        p.info,
                        p.categories()
                            .Where(c => c.show == categoriesshow.not_hidden)
                            .OrderByDescending(c => c)
                            .Select(c => new { c.title, c.sortkeyprefix })
                            .ToEnumerable())
                );

            //(source);
        }

        public struct weatherInfo
        {
            public string weatherSmile;
            public string definition;
        }

        public static weatherInfo WeatherUnicode(int code)
        {
            weatherInfo info = new weatherInfo();
            switch (code)
            {
                case 0:
                    info.weatherSmile = "☀";
                    info.definition = "Ясно";
                    return info;
                case 1:
                    info.weatherSmile = "🌤";
                    info.definition = "Преимущественно ясно";
                    return info;
                case 2:
                    info.weatherSmile = "⛅";
                    info.definition = "Переменная облачность";
                    return info;
                case 3:
                    info.weatherSmile = "☁";
                    info.definition = "Пасмурно";
                    return info;
                case 45:
                    info.weatherSmile = "🌫";
                    info.definition = "Туманно";
                    return info;
                case 48:
                    info.weatherSmile = "🌫";
                    info.definition = "Туманно с изморозью";
                    return info;
                case 51:
                    info.weatherSmile = "🌧";
                    info.definition = "Слабая непрерывная морось";
                    return info;
                case 53:
                    info.weatherSmile = "🌧";
                    info.definition = "Умеренная непрерывная морось";
                    return info;
                case 55:
                    info.weatherSmile = "🌧";
                    info.definition = "Сильная непрерывная морось";
                    return info;
                case 56:
                    info.weatherSmile = "🌧";
                    info.definition = "Слабая непрерывная морось, образующая гололёд";
                    return info;
                case 57:
                    info.weatherSmile = "🌦";
                    info.definition = "Сильная непрерывная морось, образующая гололёд";
                    return info;
                case 61:
                    info.weatherSmile = "🌧";
                    info.definition = "Непрерывный слабый дождь";
                    return info;
                case 63:
                    info.weatherSmile = "🌧";
                    info.definition = "Непрерывный умеренный дождь";
                    return info;
                case 65:
                    info.weatherSmile = "🌧";
                    info.definition = "Непрерывный сильный дождь";
                    return info;
                case 66:
                    info.weatherSmile = "🌧";
                    info.definition = "Слабый дождь, образующий гололёд";
                    return info;
                case 67:
                    info.weatherSmile = "🌧";
                    info.definition = "Сильный дождь, образующий гололёд";
                    return info;
                case 71:
                    info.weatherSmile = "❄";
                    info.definition = "Непрерывный слабый снег";
                    return info;
                case 73:
                    info.weatherSmile = "❄";
                    info.definition = "Непрерывный умеренный снег";
                    return info;
                case 75:
                    info.weatherSmile = "❄";
                    info.definition = "Непрерывный сильный снег";
                    return info;
                case 77:
                    info.weatherSmile = "🌨";
                    info.definition = "Снежные зёрна, возможен туман";
                    return info;
                case 80:
                    info.weatherSmile = "🌧";
                    info.definition = "Слабый ливневый дождь";
                    return info;
                case 81:
                    info.weatherSmile = "🌧";
                    info.definition = "Умеренный ливневый дождь";
                    return info;
                case 82:
                    info.weatherSmile = "🌧";
                    info.definition = "Сильный ливневый дождь";
                    return info;
                case 85:
                    info.weatherSmile = "🌨";
                    info.definition = "Слабый ливневый снег";
                    return info;
                case 86:
                    info.weatherSmile = "🌨";
                    info.definition = "Сильный ливневый снег";
                    return info;
                case 95:
                    info.weatherSmile = "⛈";
                    info.definition = "Умеренная гроза с дождём";
                    return info;
                case 96:
                    info.weatherSmile = "⛈";
                    info.definition = "Слабая гроза с градом";
                    return info;
                case 99:
                    info.weatherSmile = "⛈";
                    info.definition = "Сильная гроза с градом";
                    return info;
                default:
                    info.weatherSmile = "-";
                    info.definition = "-";
                    return info;
            }
        }

        public static string SetSignToTemp(double temp)
        {
            if (temp > 0)
                return "+" + temp.ToString();
            else
                return temp.ToString();
        }

        public static string GradToDirection(double grad)
        {
            if (grad > 337.5 || grad <= 22.5)
                return "⇓ с севера";
            else if (grad > 22.5 && grad <= 67.5)
                return "⇙ с северо-востока";
            else if (grad > 67.5 && grad <= 112.5)
                return "⇐ с востока";
            else if (grad > 112.5 && grad <= 157.5)
                return "⇖ с юго-востока";
            else if (grad > 157.5 && grad <= 202.5)
                return "⇑ с юга";
            else if (grad > 202.5 && grad <= 247.5)
                return "⇗ с юго-запада";
            else if (grad > 247.5 && grad <= 292.5)
                return "⇒ с запада";
            else
                return "⇘ с северо-запада";
        }

        public class CurrentWeather
        {
            public double temperature { get; set; }
            public double windspeed { get; set; }
            public double winddirection { get; set; }
            public int weathercode { get; set; }
            public int is_day { get; set; }
            public string time { get; set; }
        }

        public class Hourly
        {
            public List<string> time { get; set; }
            public List<double> temperature_2m { get; set; }
            public List<int> relativehumidity_2m { get; set; }
            public List<double> rain { get; set; }
            public List<double> snowfall { get; set; }
            public List<double> pressure_msl { get; set; }
            public List<double> visibility { get; set; }
        }

        public class HourlyUnits
        {
            public string time { get; set; }
            public string temperature_2m { get; set; }
            public string relativehumidity_2m { get; set; }
            public string rain { get; set; }
            public string snowfall { get; set; }
            public string pressure_msl { get; set; }
            public string visibility { get; set; }
        }

        public class Weather_Root
        {
            public double latitude { get; set; }
            public double longitude { get; set; }
            public double generationtime_ms { get; set; }
            public int utc_offset_seconds { get; set; }
            public string timezone { get; set; }
            public string timezone_abbreviation { get; set; }
            public double elevation { get; set; }
            public CurrentWeather current_weather { get; set; }
            public HourlyUnits hourly_units { get; set; }
            public Hourly hourly { get; set; }
        }


        public class Result_1
        {
            public int id { get; set; }
            public string name { get; set; }
            public double latitude { get; set; }
            public double longitude { get; set; }
            public double elevation { get; set; }
            public string feature_code { get; set; }
            public string country_code { get; set; }
            public int admin1_id { get; set; }
            public int admin3_id { get; set; }
            public int admin4_id { get; set; }
            public string timezone { get; set; }
            public int population { get; set; }
            public List<string> postcodes { get; set; }
            public int country_id { get; set; }
            public string country { get; set; }
            public string admin1 { get; set; }
            public string admin2 { get; set; }
            public string admin3 { get; set; }
            public string admin4 { get; set; }
        }

        public class City_Root
        {
            public List<Result_1> results { get; set; }
            public double generationtime_ms { get; set; }
        }

        public class Hourly_air
        {
            public List<string> time { get; set; }
            public List<double> pm10 { get; set; }
            public List<double> pm2_5 { get; set; }
            public List<double> carbon_monoxide { get; set; }
            public List<double> nitrogen_dioxide { get; set; }
            public List<double> sulphur_dioxide { get; set; }
            public List<double> ozone { get; set; }
            public List<double> aerosol_optical_depth { get; set; }
            public List<double> dust { get; set; }
            public List<double> uv_index { get; set; }
        }

        public class HourlyUnits_air
        {
            public string time { get; set; }
            public string pm10 { get; set; }
            public string pm2_5 { get; set; }
            public string carbon_monoxide { get; set; }
            public string nitrogen_dioxide { get; set; }
            public string sulphur_dioxide { get; set; }
            public string ozone { get; set; }
            public string aerosol_optical_depth { get; set; }
            public string dust { get; set; }
            public string uv_index { get; set; }
        }

        public class Air_Root
        {
            public double latitude { get; set; }
            public double longitude { get; set; }
            public double generationtime_ms { get; set; }
            public int utc_offset_seconds { get; set; }
            public string timezone { get; set; }
            public string timezone_abbreviation { get; set; }
            public HourlyUnits_air hourly_units { get; set; }
            public Hourly_air hourly { get; set; }
        }

        public static Air_Root get_air(string CityName = "Berlin", string lat = "52.52", string lon = "13.41")
        {
            City_Root CityClass;
            string res = GetHttpRequest("https://geocoding-api.open-meteo.com/v1/search?name=" + CityName + "&count=1&language=ru&format=json");
            bool containsSearchResult = res.Contains("results");
            if (containsSearchResult)
            {
                CityClass = JsonConvert.DeserializeObject<City_Root>(res);
                send_message = " || Воздух в городе ";
                if (CityClass.results[0].admin4 != null)
                    send_message += CityClass.results[0].admin4;
                else
                    send_message += CityClass.results[0].name;
                if (CityClass.results[0].admin3 != null)
                    send_message += ", " + CityClass.results[0].admin3;
                if (CityClass.results[0].admin2 != null)
                    send_message += ", " + CityClass.results[0].admin2;
                if (CityClass.results[0].admin1 != null)
                    send_message += ", " + CityClass.results[0].admin1;
                send_message += ": " + '\n';
            }
            else
            {
                return null;
            }
            lat = CityClass.results[0].latitude.ToString().Replace(',', '.');
            lon = CityClass.results[0].longitude.ToString().Replace(',', '.');

            string http_air = "https://air-quality-api.open-meteo.com/v1/air-quality?latitude=" + lat + "&longitude=" + lon + "&hourly=pm10,pm2_5,carbon_monoxide,nitrogen_dioxide,sulphur_dioxide,ozone,aerosol_optical_depth,dust,uv_index";
            Air_Root Air_class = JsonConvert.DeserializeObject<Air_Root>(GetHttpRequest(http_air));
            return Air_class;
        }

        public static Weather_Root get_weather(string CityName = "Berlin", string lat = "52.52", string lon = "13.41")
        {
            //string http_city = "https://geocoding-api.open-meteo.com/v1/search?name=" + CityName + "&count=1&language=ru&format=json";
            City_Root CityClass;
            string res = GetHttpRequest("https://geocoding-api.open-meteo.com/v1/search?name=" + CityName + "&count=1&language=ru&format=json");
            //(res);
            bool containsSearchResult = res.Contains("results");
            if (containsSearchResult)
            {
                CityClass = JsonConvert.DeserializeObject<City_Root>(res);
                MESSAGE_INFO.send_message = " || Погода в городе ";
                if (CityClass.results[0].admin4 != null)
                    MESSAGE_INFO.send_message += CityClass.results[0].admin4;
                else
                    MESSAGE_INFO.send_message += CityClass.results[0].name;
                if (CityClass.results[0].admin3 != null)
                    MESSAGE_INFO.send_message += ", " + CityClass.results[0].admin3;
                if (CityClass.results[0].admin2 != null)
                    MESSAGE_INFO.send_message += ", " + CityClass.results[0].admin2;
                if (CityClass.results[0].admin1 != null)
                    MESSAGE_INFO.send_message += ", " + CityClass.results[0].admin1;
                MESSAGE_INFO.send_message += ": " + '\n';
            }
            else
            {
                return null;
            }
            lat = CityClass.results[0].latitude.ToString().Replace(',', '.');
            lon = CityClass.results[0].longitude.ToString().Replace(',', '.');

            string weather_city = "https://api.open-meteo.com/v1/forecast?latitude=" + lat + "&longitude=" + lon + "&hourly=temperature_2m,relativehumidity_2m,rain,snowfall,pressure_msl,visibility&current_weather=true&windspeed_unit=ms";
            Weather_Root WeatherClass = JsonConvert.DeserializeObject<Weather_Root>(GetHttpRequest(weather_city));
            return WeatherClass;
        }

        public static string GetHttpRequest(string url)
        {
            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                Stream resStream = response.GetResponseStream();
                StreamReader reader = new StreamReader(resStream);
                string text = reader.ReadToEnd();
                return text;
            }
            catch (NullReferenceException e)
            {
                //("IM HERE");
                return "ERROR";
            }
        }
        
        private static void OnAuthorizationStateUpdated(TdApi.AuthorizationState authorizationState)
        {
            if (authorizationState != null)
            {
                _authorizationState = authorizationState;
            }
            if (_authorizationState is TdApi.AuthorizationStateWaitTdlibParameters)
            {
                TdApi.SetTdlibParameters request = new TdApi.SetTdlibParameters();
                request.DatabaseDirectory = "tdlib";
                request.UseMessageDatabase = true;
                request.UseSecretChats = true;
                request.ApiId = 20098032;
                request.ApiHash = "b86e1a3aee17d2fcacdfc15cfdb2f323";
                request.SystemLanguageCode = "en";
                request.DeviceModel = "Desktop";
                request.ApplicationVersion = "1.0";
                request.EnableStorageOptimizer = true;

                _client.Send(request, new AuthorizationRequestHandler());
            }
            else if (_authorizationState is TdApi.AuthorizationStateWaitPhoneNumber)
            {
                string phoneNumber = ReadLine("Please enter phone number: ");
                _client.Send(new TdApi.SetAuthenticationPhoneNumber(phoneNumber, null), new AuthorizationRequestHandler());
            }
            else if (_authorizationState is TdApi.AuthorizationStateWaitEmailAddress)
            {
                string emailAddress = ReadLine("Please enter email address: ");
                _client.Send(new TdApi.SetAuthenticationEmailAddress(emailAddress), new AuthorizationRequestHandler());
            }
            else if (_authorizationState is TdApi.AuthorizationStateWaitEmailCode)
            {
                string code = ReadLine("Please enter email authentication code: ");
                _client.Send(new TdApi.CheckAuthenticationEmailCode(new TdApi.EmailAddressAuthenticationCode(code)), new AuthorizationRequestHandler());
            }
            else if (_authorizationState is TdApi.AuthorizationStateWaitOtherDeviceConfirmation state)
            {
                Console.WriteLine("Please confirm this login link on another device: " + state.Link);
            }
            else if (_authorizationState is TdApi.AuthorizationStateWaitCode)
            {
                string code = ReadLine("Please enter authentication code: ");
                _client.Send(new TdApi.CheckAuthenticationCode(code), new AuthorizationRequestHandler());
            }
            else if (_authorizationState is TdApi.AuthorizationStateWaitRegistration)
            {
                string firstName = ReadLine("Please enter your first name: ");
                string lastName = ReadLine("Please enter your last name: ");
                _client.Send(new TdApi.RegisterUser(firstName, lastName), new AuthorizationRequestHandler());
            }
            else if (_authorizationState is TdApi.AuthorizationStateWaitPassword)
            {
                string password = ReadLine("Please enter password: ");
                _client.Send(new TdApi.CheckAuthenticationPassword(password), new AuthorizationRequestHandler());
            }
            else if (_authorizationState is TdApi.AuthorizationStateReady)
            {
                _haveAuthorization = true;
                _gotAuthorization.Set();
            }
            else if (_authorizationState is TdApi.AuthorizationStateLoggingOut)
            {
                _haveAuthorization = false;
                Print("Logging out");
            }
            else if (_authorizationState is TdApi.AuthorizationStateClosing)
            {
                _haveAuthorization = false;
                Print("Closing");
            }
            else if (_authorizationState is TdApi.AuthorizationStateClosed)
            {
                Print("Closed");
                if (!_needQuit)
                {
                    _client = CreateTdClient(); // recreate _client after previous has closed
                }
                else
                {
                    _canQuit = true;
                }
            }
            else
            {
                Print("Unsupported authorization state:" + _newLine + _authorizationState);
            }
        }

        private static void Print(string str)
        {
            if (_currentPrompt != null)
            {
                Console.WriteLine();
            }
            Console.WriteLine(str);
            if (_currentPrompt != null)
            {
                Console.Write(_currentPrompt);
            }
        }

        private static string ReadLine(string str)
        {
            Console.Write(str);
            _currentPrompt = str;
            var result = Console.ReadLine();
            _currentPrompt = null;
            return result;
        }

        private static void sendMessage(long chatId, string message)
        {
            // initialize reply markup just for testing
            TdApi.InlineKeyboardButton[] row = { new TdApi.InlineKeyboardButton("https://telegram.org?1", new TdApi.InlineKeyboardButtonTypeUrl()), new TdApi.InlineKeyboardButton("https://telegram.org?2", new TdApi.InlineKeyboardButtonTypeUrl()), new TdApi.InlineKeyboardButton("https://telegram.org?3", new TdApi.InlineKeyboardButtonTypeUrl()) };
            TdApi.ReplyMarkup replyMarkup = new TdApi.ReplyMarkupInlineKeyboard(new TdApi.InlineKeyboardButton[][] { row, row, row });

            TdApi.InputMessageContent content = new TdApi.InputMessageText(new TdApi.FormattedText(message, null), false, true);
            _client.Send(new TdApi.SendMessage(chatId, 0, 0, null, replyMarkup, content), _defaultHandler);
        }

        private static void sendMessage(long chatId, TdApi.InputMessageContent message)
        {
            // initialize reply markup just for testing
            TdApi.InlineKeyboardButton[] row = { new TdApi.InlineKeyboardButton("https://telegram.org?1", new TdApi.InlineKeyboardButtonTypeUrl()), new TdApi.InlineKeyboardButton("https://telegram.org?2", new TdApi.InlineKeyboardButtonTypeUrl()), new TdApi.InlineKeyboardButton("https://telegram.org?3", new TdApi.InlineKeyboardButtonTypeUrl()) };
            TdApi.ReplyMarkup replyMarkup = new TdApi.ReplyMarkupInlineKeyboard(new TdApi.InlineKeyboardButton[][] { row, row, row });

            _client.Send(new TdApi.SendMessage(chatId, 0, 0, null, replyMarkup, message), _defaultHandler);
            
            MESSAGE_INFO.can_change = true;
        }

        private static void sendReplyMessage(long chtId, long replId, string message)
        {

            can_send = false;
            // initialize reply markup just for testing
            TdApi.InlineKeyboardButton[] row = { new TdApi.InlineKeyboardButton("https://telegram.org?1", new TdApi.InlineKeyboardButtonTypeUrl()), new TdApi.InlineKeyboardButton("https://telegram.org?2", new TdApi.InlineKeyboardButtonTypeUrl()), new TdApi.InlineKeyboardButton("https://telegram.org?3", new TdApi.InlineKeyboardButtonTypeUrl()) };
            TdApi.ReplyMarkup replyMarkup = new TdApi.ReplyMarkupInlineKeyboard(new TdApi.InlineKeyboardButton[][] { row, row, row });

            TdApi.InputMessageContent content = new TdApi.InputMessageText(new TdApi.FormattedText(message, null), false, true);
            //("-\nSend message:\nchat id: " + chtId + "\nreply id: " + replId + "\ntext: " + message);
            _client.Send(new TdApi.SendMessage(chtId, 0, replId, null, replyMarkup, content), _defaultHandler);
            
            MESSAGE_INFO.can_change = true;
            can_send = true;
        }

        private static void sendReplyMessage(long chtId, long replId, TdApi.InputMessageContent message)
        {
            can_send = false;
            // initialize reply markup just for testing
            TdApi.InlineKeyboardButton[] row = { new TdApi.InlineKeyboardButton("https://telegram.org?1", new TdApi.InlineKeyboardButtonTypeUrl()), new TdApi.InlineKeyboardButton("https://telegram.org?2", new TdApi.InlineKeyboardButtonTypeUrl()), new TdApi.InlineKeyboardButton("https://telegram.org?3", new TdApi.InlineKeyboardButtonTypeUrl()) };
            TdApi.ReplyMarkup replyMarkup = new TdApi.ReplyMarkupInlineKeyboard(new TdApi.InlineKeyboardButton[][] { row, row, row });
            //("-\nSend message:\nchat id: " + chtId + "\nreply id: " + replId + "\ntext: " + message);
            _client.Send(new TdApi.SendMessage(chtId, 0, replId, null, replyMarkup, message), _defaultHandler);
            
            MESSAGE_INFO.can_change = true;
            can_send = true;
        }
    }
}