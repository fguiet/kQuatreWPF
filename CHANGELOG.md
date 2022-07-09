# Changelog

## [2022.1.0.1] - 2022/07/09

* Improve Transceiver USB Detection

## [2022.1.0.0] - 2022/07/08

* Move from .NET 5 to .NET 6
* Library update : NLog 4.7.1 to NLog 5.0.1
* Library update : EPPlus 5.7.2 to EPPlus 6.0.5
* Library update : SerialPortStream 2.3.1 to SerialPortStream 2.4.0
* Library update : NAudio 2.0.1 to SerialPortStream 2.1.0
* Library update : System.Management 5.0.0 to SerialPortStream 6.0.0



## [2021.2.1.3] - 2021/09/11

Bugs Fixing

* Receptors where not reset properly when user was creating a new firework
* Application was crashing when user wanted to add a new line 

New functionalities

* Rescue lines are added to the end when designing a new firework
* Rescue lines appear in blue

## [2021.2.1.2] - 2021/08/18

* Improve UI refresh speed (automatic time indicator)

## [2021.2.1.1] - 2021/07/28

Bugs Fixing

* Restart MP3 Audio File
* Improve UI refresh speed
* NullPointerException
* Fix regression (first line of a grid was not visible anymore)
* Reset line launched counter when line is reset

New functionalities

* Add valid / Not valid icon to determine whether firework is ready to be launched
* Change firework timeline alternate color background to White (improve speed)

## [2021.2.1.0] - 2021/07/27

* Add MP3 soundtrack playback functionality 

## [2021.2.0.0] - 2021/07/25

* Move from .NET Framework 4.8 to .NET 5
* Library update : NLog 4.7.0 to NLog 4.7.1
* Library update : EPPlus 5.1.0 to EPPlus 5.7.2
* Library update : SerialPortStream 2.2.0 to SerialPortStream 2.3.1
* Library update : Telerik UI for WPF R1 2020 SP1 to Telerik UI for WPF R2 2021

## [2021.1.1.0] - 2021/07/24

* Add current firework time indicator on UI
* Remove unused using statement
* Prevent user from using File menu while firework is launched
* Fix some bugs when adding rescue line
* Refactor automatic timeline scrolling
* Add center time line button (allow user to center screen on current firework time)