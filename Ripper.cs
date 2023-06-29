using System;
using System.Collections.Specialized;
using System.IO;

/*
 *  by Alsch092 @ github
 *  feel free to make improvements, fork, or raise issues.
 *  */


namespace RedmineRipper
{
    //this class provides capability for iterating through the issues in a redmine and saving an attachment (if it has one)
    public class Ripper //pulling all issues as pdf works, now we need to scan issues for attachments then download all of those, or just go 1 by 1 
    {
        //static string redmine_path = @"redmine/"; //some implementations use /redmine/ as the start of the url paths
        static string issue_path = @"issues/";
        static string login_path = @"login?back_url="; //get rid of IP fragment on release
        static string logout_path = @"logout";

        string base_url;
        string authenticity_token; //88 characters, looks like some base64 string

        int currentIssueCounter = 0;

        const int minimum_landing_page_length = 3290; //the authenticity_token cookie is on at least position 3290 on the implementation i'm testing on -> for your implementation, you need to find the second occurance of "authenticity_token" in the response, where the value is set.
        const int minimum_issue_length = 20000;

        CookieAwareWebClient client = new CookieAwareWebClient();

        public enum ExportTypes
        {
            atom,
            pdf,
            html //not yet implemented, requires us to save the page response into a file
        }


        public Ripper(string url)
        {
            base_url = url;
        }

        public void Orchestrate(string user, string pass, int startIssue, int maxIssue, bool exporting, ExportTypes exportFileType, bool downloadingAttachments)
        {
            if(LoginRedmine(user, pass))
            {
                for(int i = startIssue; i < maxIssue; i++)
                {
                    currentIssueCounter = i;

                    if(GetIssue(currentIssueCounter, downloadingAttachments))
                    {                  
                        if(exporting)
                        {
                            Console.WriteLine("Attempting to export issue {0}...", currentIssueCounter);  
                            Export(currentIssueCounter, exportFileType);
                        }        
                    }
                }

                Logout();
            }
        }

        public bool Logout()
        {
            var logout_values = new NameValueCollection
                {
                    { "_method", "post" },
                    { "authenticity_token", authenticity_token }
                };

            try
            {
                byte[] logoutResponse = client.UploadValues(base_url + logout_path, logout_values);
                string result = System.Text.Encoding.UTF8.GetString(logoutResponse);

                //parse response if you want
                if (result.Contains("redirected"))
                    return true;
            }
            catch
            {
                Console.WriteLine("Sending Logout packet failed! Make sure the connection is available and that authenticity_token is correct!");
            }
           
            return false;
        }

        public void Export(int issueNumber, ExportTypes fileExtension)
        {
            using(client)
            {
                string local = "./issues/" + Convert.ToString(issueNumber) + "." + fileExtension;
                string address = base_url + issue_path + Convert.ToString(issueNumber) + "." + fileExtension;

                try
                {
                    if (fileExtension == ExportTypes.pdf || fileExtension == ExportTypes.atom)
                    {
                        client.DownloadFile(address, local);
                    }
                    else if(fileExtension == ExportTypes.html)
                    {
                        string response = client.DownloadString(address);

                        using (StreamWriter writer = new StreamWriter(local, false))
                        {
                            writer.Write(response);
                            writer.Close();
                        }
                    }
                }
                catch
                {
                    Console.WriteLine("Couldn't download file at issue: {0}", issueNumber);

                    using (StreamWriter writer = new StreamWriter("./Errors.txt", true))
                    {
                        writer.WriteLine("Couldn't download file at issue: " + Convert.ToString(issueNumber));
                    }
                }
            }
        }

        public void DownloadAttachment(string attach_url) //fix
        {
            using (client)
            {
                string local = Convert.ToString(attach_url.Substring(1)); //skips first / to avoid doing //
                string address = base_url + (attach_url.Substring(1)); //skip first char
                string filename = Path.GetFileName("./" + local.Substring(1));
                int counter = 2;
               
                while (File.Exists("./attachments/" + filename))
                {
                    Console.WriteLine("File already exists!");
                    filename = filename + "_" + counter;
                    counter++;
                }

                try
                {
                    Console.WriteLine("Attempting to download: {0}", filename);
                    client.DownloadFile(address, "./attachments/" + filename);
                }
                catch
                {
                    Console.WriteLine("Couldn't download file at issue: {0}", attach_url);

                    using (StreamWriter writer = new StreamWriter("./Errors.txt", true))
                    {
                        writer.WriteLine("Couldn't download attachment at url: " + Convert.ToString(attach_url));
                    }
                }
            }
        }

        public string GetAuthToken(string page)
        {
            int index_authtoken = page.IndexOf("authenticity_token\" value=");
            string authenticity_token = page.Substring(index_authtoken + ("authenticity_token\" value=").Length + 1, 88);
            return authenticity_token;
        }

        public int GetAttachmentIndex(string page, int startIndex)
        {
            int index = 0;

            try
            {
                int attachment_index = page.IndexOf("\"/attachments/download/", startIndex); //need to extract the attachment number out of url
                int end_index = page.IndexOf("\">", attachment_index);

                if (attachment_index > 0)
                {
                    if (end_index > attachment_index)
                    {
                        index = attachment_index;
                    }
                }
            }
            catch
            {
                return 0;
            }

            return index;
        }

        public string GetAttachmentUrl(string page, int startIndex)
        {
            string attachment_url = "";

            try
            {
                int attachment_index = page.IndexOf("\"/attachments/download/", startIndex); //need to extract the attachment number out of url
                int end_index = page.IndexOf("\">", attachment_index);

                if (attachment_index > 0)
                {
                    if (end_index > attachment_index)
                    {
                        attachment_url = page.Substring(attachment_index, end_index - attachment_index);
                    }
                }
            }
            catch
            {
                return "";
            }


            return attachment_url;
        }

        public bool GetIssue(int issueNumber, bool downloadAttachments)
        {
            using (client)
            {
                string issuePage;

                try
                {
                    issuePage = client.DownloadString(base_url + issue_path + Convert.ToString(issueNumber));
                }
                catch
                {
                    Console.WriteLine("Issue page {0} got 404 or some connection issue", issueNumber);
                    return false;
                }

                if(issuePage.Length > minimum_issue_length) 
                {
                    authenticity_token = GetAuthToken(issuePage); //needs to be set each new page iirc

                    if(downloadAttachments)
                    {
                        int attach_index = GetAttachmentIndex(issuePage, 0);
                        string attach_url = GetAttachmentUrl(issuePage, 0);

                        int nAttachments = 0;

                        if (attach_url.Length > 1)
                        {
                            DownloadAttachment(attach_url);
                            nAttachments++;

                            int next_attach_index = 0;
                            do
                            {
                                next_attach_index = GetAttachmentIndex(issuePage, attach_index + 1);

                                if (next_attach_index > 0)
                                {
                                    string next_attach_url = GetAttachmentUrl(issuePage, next_attach_index);
                                    DownloadAttachment(next_attach_url);
                                    attach_index = next_attach_index;
                                }

                            } while (next_attach_index != 0);
                        }
                    }

                    return true;
                }
            }

            return false;
        }

        public bool LoginRedmine(string user, string pass)
        {
            if (base_url.Length == 0)
            {
                Console.WriteLine("No base url specified.");
                return false;
            }

            if(user.Length == 0 || pass.Length == 0)
            {
                Console.WriteLine("username or password was empty!");
                return false;
            }

            string landingpage = client.DownloadString(base_url);

            if(landingpage.Length >= minimum_landing_page_length) //the parameter authenticity_token is located on atleast index 3290 on the implementation i'm using
            {
                authenticity_token = GetAuthToken(landingpage); //position 3298 holds authenticity_token
                Console.WriteLine("Auth token: " + authenticity_token);
            }
            else
            {
                Console.WriteLine("username or password was empty!");
                return false;
            }

            using (client)
            {
                var values = new NameValueCollection
                {
                    { "utf8", "%E2%9C%93" }, //i don't know why redmine uses a "Checkmark" symbol for this instead of just '1'
                    { "authenticity_token", authenticity_token },
                    { "back_url", base_url },
                    { "username", user },
                    { "password" , pass},
                    {"login", "Login+%C2%BB" } //weird field
                };

                string full_url = base_url + login_path + base_url;
                byte[] loginResponse = client.UploadValues(full_url, values);
                string result = System.Text.Encoding.UTF8.GetString(loginResponse); //should be the default page after logging in, not the response from the re-direction itself

                if (result.Contains(user)) //if we can see our username inside the response, it means we should be logged in. this might not be foolproof to usernames such as 'redmine'!
                {  
                    Console.WriteLine("Successfully logged in!");
                    Console.WriteLine(result);
                    return true;
                }

                Console.WriteLine("Failed to log in..?");
            }

            return false;
        }
    }
}
