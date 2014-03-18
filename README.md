JourFix
=======


Description
-----------
This is mainly an internal repository. The code governs a SharePoint workspace
that is used for weekly reports from department heads. It contains a SharePoint
feature that adjusts the permission on form libraries and task lists so that
only the assigned department head and the general manager can access the item.
The InfoPath form on the other hand contains the logic to save the filled-out
form without any further user interaction as well as querying the SharePoint
people web service for the current user and cross-referencing the obtained
user's department with a settings list to setup the form.
In addition the form also includes a connection to idea and error management
lists and filters and displays them according to the department.


What can I do with it?
----------------------
While the SharePoint feature might be quite helpful and can be used in other
scenarios than weekly reports, the InfoPath form is - apart from being in
German - heavily tailored to the company it was made for. Yet if you drop the
idea and error management the only thing required is to setup your own settings
list.
