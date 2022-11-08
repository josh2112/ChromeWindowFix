# ChromeWindowFix
Automatically adjusts the Y position of 'snapped' Chrome windows so they behave like maximized ones.

When maximised, Chrome tabs are flush with the top of the screen, making it easy to slam your mouse to the top of the screen and select a tab. When not maximized, Chrome puts a 9-pixel border between the tabs and the top of the window. For windows that are 'snapped' to a predefined location this can be quite annoying, because the window is flush with the top of the screen but the tabs aren't.

This app sits in your system tray and checks the position of all Chrome windows once per second. If they're flush with the top of the screen but not maximized, the window position is adjusted a few pixels up so that tabs are flush with the top of the screen.

The core of the app is the `WindowPositionFixer` class, which takes three `Func`s:
 - a `Process` filter to select the `Process`es we're interested in,
 - a needs-fix selector that looks at the window size & position to determine if it needs to be adjusted, and
 - a fix-window function that returns an adjusted rectangle for the window.

These lambdas could probably be entirely specified in JSON to make a customzable solution; I just haven't gotten to that yet.