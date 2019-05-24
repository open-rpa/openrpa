# TypeText syntax

![](C:\code\openrpa\docs\img\1558718403772.png)

Typetext sends the text from the Property Text as a sequence of keys, to what ever application has focus right now (or windows, if none)

Use {} to send special keys, for instance to press Left Windows key type "{LWin}".

Often you want to combine keys, pressing and holding them one at the time. There are 2 ways of doing that, each key be presented with either up or down, to present pressing or releasing the key, so for instance, to copy a text you could write either

"{LCONTROL down}c{LCONTROL up}"

or more read friendly as

"{LCONTROL, V}"

or if your really want to control the flow, you could use 

"{LCONTROL down}{KEY_V down}{LCONTROL up}{KEY_V up}"

