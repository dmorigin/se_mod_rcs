# Rover Control System (RCS)

Author: DMOrigin

Page: https://www.gamers-shell.de/

This is a script for a programable block in Space Engineers. It is designed to manage a couple of things in a rover or any other vehicle like a rover. Any operations are split up in modules. So it is possible to deactivate or activate single modules. For more details about the modules read the description further down.

## Space Engineers

Space Engineers is a Game developed by Keen Software House. You can find more details about Keen Software House at his official Web Site at https://www.keenswh.com/. Or visit his forum directly at https://forums.keenswh.com/.

## Modules

The system is designed around modules. Every operation or group of operations are implemented as a module. The base of this script manageing these modules and execute them. Some of the modules has dependencies to other modules. In this case a modules is waiting for it's dependencies before running.



### Main RCS System

This module containes some basic informations about the rover himself. This can be general states like "Parking", cockpit and something like that. The most other modules depend on this module. So if you deactivate this module all the other ones are also stopped.

### Docking Controller

The docking controller is designed to manage all of your Connectors on your rover. The main functionality is to automaticaly lock or unlock the Connector Block. It can be also manage lights to visualize the state of your Connector. It pronounces the state of any Connector block to other modules. So other modules can be react on them.

### Suspensions Manager

### Energie Manager

### Inventory Manager
