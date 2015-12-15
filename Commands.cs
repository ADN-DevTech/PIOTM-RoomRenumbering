#region Imported Namespaces

//.NET common used namespaces
using System;
using System.Windows.Forms;
using System.Collections.Generic;

//Revit.NET common used namespaces
using Autodesk.Revit.ApplicationServices;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Autodesk.Revit.DB.Architecture;

#endregion

namespace RoomRenumbering
{
  [Transaction(TransactionMode.Automatic)]
  [Regeneration(RegenerationOption.Manual)]
  public class Commands : IExternalCommand
  {
    public class MySelectionFilter : ISelectionFilter
    {
      public bool AllowElement(Element elem)
      {
        return elem is Room;
      }

      public bool AllowReference(Reference reference, XYZ position)
      {
        return true;
      }
    }

    /// <summary>
    /// The one and only method required by the IExternalCommand interface,
    /// the main entry point for every external command.
    /// </summary>
    /// <param name="commandData">Input argument providing access to the Revit application and its documents and their properties.</param>
    /// <param name="message">Return argument to display a message to the user in case of error if Result is not Succeeded.</param>
    /// <param name="elements">Return argument to highlight elements on the graphics screen if Result is not Succeeded.</param>
    /// <returns>Cancelled, Failed or Succeeded Result code.</returns>
    public Result Execute(
        ExternalCommandData commandData,
        ref string message,
        ElementSet elements)
    {
      UIDocument uidoc = commandData.Application.ActiveUIDocument;

      try
      {
        int roomNumber = 1;

        while (true)
        {
          Reference selRef = uidoc.Selection.PickObject(
            ObjectType.Element, 
            new MySelectionFilter(), 
            "Select a room");

          Room room = (Room)selRef.Element;
          // In 2012 it's better to use
          // Room room = (Room)uidoc.Document.GetElement(selRef);   

          FilteredElementCollector collector = new FilteredElementCollector(uidoc.Document);

          // either
          // collector.OfClass(typeof(Enclosure));  
          // or
          collector.WherePasses(new RoomFilter());

          ParameterValueProvider provider = new ParameterValueProvider(
            new ElementId(BuiltInParameter.ROOM_NUMBER));

          FilterStringEquals evaluator = new FilterStringEquals();

          FilterStringRule rule = new FilterStringRule(
            provider,
            evaluator,
            roomNumber.ToString(),
            false);

          ElementParameterFilter filter = new ElementParameterFilter(rule); 

          collector.WherePasses(filter);

          IList<Element> rooms = collector.ToElements();

          if (rooms.Count > 0)
            ((Room)rooms[0]).Number = room.Number;

          room.Number = roomNumber.ToString();

          uidoc.Document.Regenerate(); 

          roomNumber++;
        }
      } catch (Autodesk.Revit.Exceptions.OperationCanceledException) {} 

      //Must return some code
      return Result.Succeeded;
    }
  }
}
