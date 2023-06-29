# RedmineScraper
A tool written in C# to download all threads + attachments from a Redmine repo.  

Logs into a redmine website and proceeds to export all issues/threads one-by-one, automatically downloading any attachments found along the way.   
This was made because I couldn't find anything similar floating around, hopefully you'll find it useful!  

Usage: ./RedmineRipper.exe <base_url> <user> <pass> <export_format>  
Example: Example: ./RedmineRipper.exe http://your-redmine-site.come/ account password .pdf  

Supports exporting of: .pdf, .atom, and .html. If output claims the issue got a 404, then there is likely no existing issue for that issue number.
Limitations: Untested for redmines where logging in is -not- required (such as the default redmine.org site), but this can easily be done by removing one line of code where login occurs: [ if(LoginRedmine(user, pass)) ]  

Example output:  
Auth token: <ommitted>  
Successfully logged in!  
Issue page 148 got 404 or some connection issue  
Attempting to export issue 149...  
Attempting to export issue 150...  
Attempting to download: attachment_1.txt  
File already exists!  Renaming to attachment_1.txt_2    
Attempting to export issue 151...  
....  
End of output  

Enjoy!  
