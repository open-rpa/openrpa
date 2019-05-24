# OpenApplication

![1558717936148](C:\code\openrpa\docs\img\1558717936148.png)

**What:** The basic idea behind this activity, is to make it easy to check for an running application, and launch it, if it is not, at the time of writing this, the activity supports Windows components, Internet Explorer and NativeMessaging (chrome/Firefox)

**How:** Click any running application/webpage and a selector for that application will be created. To fine tune, click Open Selector and make your modifications (like what elements to look for) To test the selector is working, click Highlight

**Why:** Use it instead of clicking though the start menu or opening files directly. The selector will include details like parameters, so if you opened word by clicking a document, the selector will detect the that too.

# HighlightElement

![1558718230861](C:\code\openrpa\docs\img\1558718230861.png)

**What:** Highlights a found element.

**How:** Drag this inside a GetElement activity, to highlight it. The workflow will continue while highlighting, to block the workflow from running while highlighting, set Clicking=True

**Why:** Use if for debugging, or use it to make cool effects while robot is running.

# TypeText

![1558718403772](C:\code\openrpa\docs\img\1558718403772.png)

**What:** To assign a value you generally should use the Value parameter an item's, but if you need to send a Key combination to an application, this is your friend. 

**How:** I strongly recommend using the Record function to use this, but in case your interested in understanding the syntax, you can [read more here](typetext-syntax.md).

**Why:** Sometimes its just easier/faster to use short cut keys, or the element will not receive the input properly.

# ClickElement

![1558720283840](C:\code\openrpa\docs\img\1558720283840.png)

**What:** Represents a mouse click, default is using virtual clicks. This means different things depending on the object type and provider, but generally you can think if it as a click without moving the mouse. Set VirtualClick=False to do a real Mouse Click and use OffsetX/OffsetY to make the click hit within the element (0,0 will be the top left corner of the element)

**How:** I strongly recommend using the Record function to use this, but if you frag this inside an GetElement activity it will automatically use the element it was placed within ( Element=item )

**Why:** Clicking is needed, not everything can be done using shortcut keys.

# GetElement

![1558720872448](C:\code\openrpa\docs\img\1558720872448.png)

**What:** The primary tool of the robot, used for locating items in the environment. Depending on the provider the settings may differ a little, but generally this can be used to locate one or many elements based on a selector. Use Open Selector to fine tune what you want. Per default we only find 1 item, and throw an error if we find less than one object, use MaxResults and MinResults to change the behevouiur. 
For instance if you want to loop though a DataSet you could select the DataRow and set MaxResults to 100. Then within the GetElement you add new GetElement's to "pick out" different elements per DataRow.
Setting MinResults to 0, effecly means your only checking if an object exists, but is not throwing an error, if nothing is found

**How:** Either press Record and click an element, the robot will try and figure out what type of element you clicked and insert the appropriate GetElement activity, or you can drag one in your self and use Open Selector to precisely select the element your interested in.

**Why:** Without elements, it's not really RPA 

Detector

![1558723009540](C:\code\openrpa\docs\img\1558723009540.png)

**What:** Make workflow wait for an detector to be triggered

**How:** Drag it into your workflow, and once it gets to this activity the workflow will go idle and wait until the selected detector gets triggered. ( state is saved, so if the robot or the machine is rebooted, the workflow will continue from this activity )

Why: This is an excellent way if making workflows react to things in the environment. You could make a workflow that helps fill out information in a form when the user presses a specific keyboard combination, or you would show a helpful dialog, when ever a user opens a Timesheet, or maybe you want to add extra actions to an existing button. 

Note, as soon as an activity has been created, all actions will also be sent to OpenFlow
![1558723403613](C:\code\openrpa\docs\img\1558723403613.png)

# OpenURL

![1558722430092](C:\code\openrpa\docs\img\1558722430092.png)

**What:** It is pretty much the same as OpenApplication, except it only supports Internet Explorer. 

**How:** Click Get current, to automatically fetch the URL of the active tab of first browser it finds.

**Why:** Not sure, OpenApplication should be good enough for now.