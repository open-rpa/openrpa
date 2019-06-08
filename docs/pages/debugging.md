---
layout: page
title: Overview of debugging
description: Overview of debugging tools
---
## WriteLine

A common way of debugging is printing out the content of different variable with information about where in the workflow this happened

![1559997791984](debugging/1559997791984.png)

![1559997838320](debugging/1559997838320.png)

That is perfectly good way of keeping an eye on how a workflow is performing and making sure everything is working as expected. But once in a while when you are hunting down a specific problem, or if you unsure how to get specific information it can be handy to use breakpoints instead.

## Breakpoints

Select any activity in a workflow and press F9. This will toggle a break point at the given location, shown with the little red icon in the top left corner.

![1559997615758](debugging/1559997615758.png)

When a workflow is running at gets to an activity with a breakpoint enabled, it will stop execution and "dump" all variable and parameters for the workflow in the inspection window. You will know where the workflow halted, but the yellow border around the activity

![1559998299970](debugging/1559998299970.png)

Notice that test is (null) and does not contain the value "hi mom". We need to let the workflow execute one more activity in order for this to happen, you do that by pressing the step over key F10.

![1559998322690](debugging/1559998322690.png)

Now test contains "hi mom" and the yellow border has moved to the next activity.

You do not need to single step over every activity, by pressing F5 ( or the Play button ) the workflow will continue as normal, and/or stop next time it hits a breakpoint. You can add and remove breakpoints even when the workflow is running or is idle.

Breakpoints also work, when workflows have been started remotely from OpenFlow, making debugging parameters from OpenFlow very easy.

![1559999293327](debugging/1559999293327.png)

![1559998760787](debugging/1559998760787.png)

You can also start a workflow by using these keys. F10 will start the workflow and stop at the first activity, where as F5 will start the workflow as if you had pressed the Play button.

