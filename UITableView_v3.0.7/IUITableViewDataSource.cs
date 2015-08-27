/// <summary>
/// zhao-zilong
/// </summary>

using System.Collections;
using UnityEngine;

/// <summary>
/// Table view cell can be touched if implement this interface.
/// </summary>
public interface IUITableViewTouchable
{
	void DidSelectedRowAtIndexInTableView (UITableView tableView, int rowIndex);
}

/// <summary>
/// Table view can be edited or not.
/// Optional interface
/// </summary>
public interface IUITableViewEditable
{
	/// <summary>
	/// Delete button has been touched and wait for your operation.
	/// At the end of this method, you should call "void ConfirmDeleteCellForRowAtIndex (int rowIndex, IEnumerator deleteAnimation)" of tableView
	/// to tell tableView play cell delete animation.
	/// </summary>
	/// <param name="tableView">Table view.</param>
	/// <param name="rowIndex">Row index.</param>
	void WillDeleteRowAtIndexInTableView (UITableView tableView, int rowIndex, GameObject cellGameObject);

	/// <summary>
	/// Delete animation had aready played and your should delete data in this method.
	/// </summary>
	/// <param name="tableView">Table view.</param>
	/// <param name="rowIndex">Row index.</param>
	void DidDeleteRowAtIndexInTableView (UITableView tableView, int rowIndex);
}

/// <summary>
/// Table view data source must be implemented.
/// </summary>
public interface IUITableViewDataSource 
{
	/// <summary>
	/// Cells for row at index in table view.
	/// </summary>
	/// <returns>The for row at index in table view.</returns>
	/// <param name="tableView">Table view.</param>
	/// <param name="rowIndex">Row index.</param>
	/// <code>
	/// string reuserIndentifier = "ReuserIndentifier";
	/// UITableViewCell cell = tableView.DequeueReusableCellWithIndentifier(reuserIndentifier);
	/// if (cell == null)
	/// {
	/// cell = tableView.AttachCellWithReuseIndentifier(PrefabContainsUITableViewCell, reuserIndentifier);
	/// cell.IsTouchable = true;
    /// }
	/// //Customize your cell
	/// </code>
	UITableViewCell CellForRowAtIndexInTableView (UITableView tableView, int rowIndex);

	/// <summary>
	/// The total number of rows(cells) in table view.
	/// </summary>
	/// <returns>The of rows in table view.</returns>
	/// <param name="tableView">Table view.</param>
	int NumberOfRowsInTableView (UITableView tableView);

	/// <summary>
	/// Heights for cell in each row in table view.
	/// </summary>
	/// <returns>The for row in table view.</returns>
	/// <param name="tableView">Table view.</param>
	/// <param name="rowIndex">Row index.</param>
	float HeightForRowInTableView (UITableView tableView, int rowIndex);

	/// <summary>
	/// Cells for row will be killed.
	/// Release resources on cell in this method.
	/// </summary>
	/// <param name="tableView">Table view.</param>
	/// <param name="rowIndex">Row index.</param>
	void CellForRowWillBeKilled (UITableView tableView, int rowIndex);
}
