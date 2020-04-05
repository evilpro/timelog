using System;
using System.IO;
using Telegram.Bot;
using timelog.Exceptions;
using timelog.Models;

namespace timelog
{
    class Program
    {
        static readonly string helpText = "Usage: timelog <start | status | stop> <description>\n" +
                                          "  - Start: Begin timetracking with the work item as title\n" +
                                          "  - Status: Issue a status update to your teammates\n" +
                                          "  - Stop: Wrap up time tracking with a summary of your work";

        static readonly string botToken = ""; //TODO: Insert Bot Token //TODO-Later: Read Bot Token from configuration
        static readonly int botChatId = 0; //TODO: Insert Chat ID //TODO-Later: Read Chat ID from configuration
        static readonly string sessionLocation = Path.Combine(Path.GetTempPath(), "session.tlg");

        static TelegramBotClient botClient;
        static void Main(string[] args)
        {
            if(args.Length < 2)
            {
                //Notify user about input error and offer help
                Console.WriteLine($"Not enough parameters specified.");
                Console.WriteLine(helpText);
                Environment.Exit(1);
            }
            else
            {
                botClient = new TelegramBotClient(botToken);
                Console.WriteLine($"Bot selftest result was {(botClient.TestApiAsync().Result ? "successful" : "unsuccessful")}");

                string mode = args[0];
                switch (mode.ToLower()) 
                {
                    #region Start Command
                    case "start":
                        if(File.Exists(sessionLocation))
                        {
                            //Read existing session
                            if(WorkSession.TryParse(sessionLocation, out WorkSession oldSession))
                            {
                                Console.WriteLine("You already have a session running.");
                                Console.WriteLine($"It was started on {new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc).AddSeconds(oldSession.StartTime).ToString()} with subject \"{oldSession.Subject}\"");
                            }
                            else
                            {
                                Console.WriteLine("You have a session of an unsupported format running.");
                            }

                            //Selection text
                            string selectionText = $"[D]iscard previous session{(oldSession != null ? " | [C]ontinue previous session" : string.Empty)}";
                            Console.WriteLine(selectionText);
                            while (true)
                            {
                                ConsoleKeyInfo key = Console.ReadKey();
                                if (key.Key == ConsoleKey.D)
                                {
                                    File.Delete(sessionLocation);
                                    StartAndSaveSession(args[1]);
                                    break;
                                }
                                else if (key.Key == ConsoleKey.C && oldSession != null)
                                    break;
                                else
                                {
                                    Console.WriteLine("You've made an invalid selection.");
                                    Console.WriteLine("selectionText");
                                }
                            }                            
                        }
                        else
                        {
                            StartAndSaveSession(args[1]);
                        }
                        
                        break;
                    #endregion
                    #region Stop Command
                    case "stop":
                        if(File.Exists(sessionLocation))
                        {
                            //Read information about the current session
                            if(WorkSession.TryParse(sessionLocation, out WorkSession session))
                            {
                                //Finalize session with the user-provided summary
                                Console.WriteLine($"Finishing current session titled \"{session.Subject}\".");
                                if(session.FinishSession(args[1]))
                                {
                                    TimeSpan span = TimeSpan.FromSeconds(session.Duration);
                                    //Notify Telegram
                                    Console.WriteLine($"Pushing update to Telegram");
                                    botClient.SendTextMessageAsync(new Telegram.Bot.Types.ChatId(botChatId), $"User *{Environment.UserName}* has stopped working on *{session.Subject}*\n\n*Summary*\n`{session.Summary}`\n\n*Total time spent*\n`{span.Days} Days {span.Hours} Hours {span.Minutes} Minutes {span.Seconds} Seconds`", Telegram.Bot.Types.Enums.ParseMode.Markdown);
                                    Console.WriteLine($"Deleting session information from disk.");
                                    File.Delete(sessionLocation);
                                }
                                else
                                {
                                    Console.WriteLine($"Your current session could not be terminated.");
                                    Environment.Exit(1);
                                }
                            }
                            else
                            {
                                Console.WriteLine($"A session using an unsupported file-format was found. Make sure you are using a correct version.");
                                Environment.Exit(1);
                            }

                        }
                        else
                        {
                            Console.WriteLine("You're currently not working on a time-tracked session.");
                            Environment.Exit(1);
                        }

                        break;
                    #endregion
                    #region Status Command
                    case "status":
                        //Check if we are tracking time
                        if(File.Exists(sessionLocation))
                        {
                            //Read information about the current session
                            if(WorkSession.TryParse(sessionLocation, out WorkSession session))
                            {
                                Console.WriteLine($"Updating status of session \"{session.Subject}\"");
                                //Fetch current time to calculate a difference & send it off to Telegram
                                if (TimeData.FetchTimeData(out TimeData data))
                                {
                                    int duration = data.unixtime - session.StartTime;
                                    TimeSpan span = TimeSpan.FromSeconds(duration);

                                    //Notify the Telegram group
                                    Console.WriteLine($"Pushing update to Telegram");
                                    botClient.SendTextMessageAsync(
                                        new Telegram.Bot.Types.ChatId(botChatId),
                                        $"User *{Environment.UserName}* has issued an update on *{session.Subject}*\n\n*Details*\n`{args[1]}`\n\n*Time spent so far*\n`{span.Days} Days {span.Hours} Hours {span.Minutes} Minutes {span.Seconds} Seconds`",
                                        Telegram.Bot.Types.Enums.ParseMode.Markdown
                                    );
                                }

                            }                            
                        }
                        else
                            //Notify about user error
                            Console.WriteLine("You're currently not working on a time-tracked session.");

                        break;
                    #endregion
                    #region Invalid Command
                    default:
                        //Notify about user error & offer help
                        Console.WriteLine("Invalid mode specified.");
                        Console.WriteLine(helpText);
                        break;
                    #endregion
                }
            }
        }

        static bool StartAndSaveSession(string subject)
        {
            Console.WriteLine($"Starting new work session with subject \"{subject}\".");
            try
            {
                WorkSession session = new WorkSession(subject);
                Console.WriteLine($"Saving session information to disk.");
                session.SaveSession(sessionLocation);
                //Notify Telegram
                botClient.SendTextMessageAsync(
                    new Telegram.Bot.Types.ChatId(botChatId),
                    $"User *{Environment.UserName}* started working on `{subject}`",
                    Telegram.Bot.Types.Enums.ParseMode.Markdown
                );

                return true;
            }
            catch (RemoteTimeUnavailableException)
            {
                Console.WriteLine("Your session could not be started because the remote time server is unavailable.");
                return false;
            }
            catch
            {
                Console.WriteLine("An unspecified error occured and your session could not be started.");
                if (File.Exists(sessionLocation))
                    File.Delete(sessionLocation);

                return false;
            }      
        }
    }
}
