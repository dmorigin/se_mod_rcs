/*
 *   Rover Control System (RCS)
 *   ---------------------------
 * 
 * Author: [DM]Origin
 * Page: https://www.gamers-shell.de/
 * 
 * This is a complete control system for a rover base vehicle. Use this to manage
 * several components of your rover, like energy, suspensions, inventory and more.
 * 
 */

/*
 * Fast way to aquire all item types
 * 
IMyConveyorSorter sorter = GridTerminalSystem.GetBlockWithName("Conveyor Sorter") as IMyConveyorSorter;
if (sorter != null)
{
    List<MyInventoryItemFilter> filters = new List<MyInventoryItemFilter>();
    sorter.GetFilterList(filters);

    foreach (var filter in filters)
    {
        Echo("Type: " + filter.ItemType.TypeId + "//" + filter.ItemType.SubtypeId);
    }
}
*/