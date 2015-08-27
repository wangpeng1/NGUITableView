using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;

[AddComponentMenu("NGUI/Interaction/Table View")]
public class UITableView : UIScrollView 
{
	/// <summary>
	/// The total invisible cell number.
	/// There are half number at top and the other half at bottom.
	/// Plus one if odd number.
	/// </summary>
	public int InvisibleCellNumber = 6;

	/// <summary>
	/// The max number of reusable cells in all queues.
	/// The number not includes invisible cells.
	/// </summary>
	public int MaxReusableCellNumber = 0;

	/// <summary>
	/// Object that implement IUITableViewDataSource interface. 
	/// Set it before reloadData.
	/// </summary>
	public IUITableViewDataSource DataSource { get; set; }

	/// <summary>
	/// Set object to this delegateTarget before reloadData if you implement IUITableViewTouchable or IUITableViewEditable
	/// </summary>
	public object DelegateTarget { get; set; }

	/// <summary>
	/// Tag which can tell different tableView.
	/// </summary>
	/// <value>The tag.</value>
	public int Tag { get; set; }

	private Dictionary<string, Queue<UITableViewCell>> _reusableCellQueueDictionary;

	private class UITableViewCellHolder 
	{
		public UITableViewCell Cell { get; set; }
	}
	private List<UITableViewCellHolder> _cellHolders;
	
	/// <summary>
	/// The first visible cell row from top.
	/// </summary>
	protected int _visibleStartRow = 0;
	/// <summary>
	/// The last visible cell row to bottom.
	/// </summary>
	protected int _visibleLastRow;

	/// <summary>
	/// The top invisible cell row.
	/// </summary>
	protected int _cacheStartRow = 0;
	/// <summary>
	/// The bottom invisible cell row.
	/// </summary>
	protected int _cacheLastRow;

	//For debug
	private int DEBUG_CacheLastRow 
	{ set { /*Debug.Log ("Last Cache Row is "+_cacheLastRow);*/ } }

	private int DEBUG_CacheStartRow 
	{ set { /*Debug.Log ("Start Cache Row is "+_cacheStartRow);*/  } }

	private int DEBUG_VisibleStartRow 
	{ set { /*Debug.Log ("Start Visible Row is "+_visibleStartRow);*/  } }

	private int DEBUG_VisibleLastRow 
	{ set { /*Debug.Log ("Last Visible Row is "+_visibleLastRow);*/  } }

	private float _lastOffSetY = 0;

	private float _visibleStartCellOffSetY = 0;

	private Bounds _invisibleBounds;

	private enum TopOrBottom
	{
		Top,
		Bottom,
	}

	private UITableViewCellStatus _cellStatus = UITableViewCellStatus.Normal; 

	// Use this for initialization
	protected override void Start () 
	{
		base.Start();

		movement = Movement.Vertical;

		mPanel.clipping = UIDrawCall.Clipping.SoftClip;

	}

	void ResetData ()
	{
		if (_cellHolders != null)
		{

			//first start index cell offset positionY
			if (_visibleStartRow > _cellHolders.Count-1) 
			{
				//There is nothing to reset
				_visibleStartCellOffSetY = 0;
			}
			else
			{
				_visibleStartCellOffSetY = Mathf.Abs((Mathf.Abs(_cellHolders[_visibleStartRow].Cell.Top) - Mathf.Abs(ContentVisibleTop())));
			}

//			Debug.Log("Position of first visible cell Position:"+_visibleStartCellOffSetY);

			foreach (var cellholder in _cellHolders)
			{
				EnqueueReusableCell(cellholder);
			}
			_cellHolders.Clear();
		}

//		ResetPosition();

		_visibleStartRow = 0;
		_visibleLastRow = 0;

		_cacheStartRow = 0;
		_cacheLastRow = 0;

		this.DEBUG_VisibleStartRow = 0;
		this.DEBUG_VisibleLastRow = 0;
		this.DEBUG_CacheStartRow = 0;
		this.DEBUG_CacheLastRow = 0;

		_lastOffSetY = 0;
	}

	void ReloadDataFromRowIndex (int fromIndex)
	{
//		ResetData();

		int numberOfCell = DataSource.NumberOfRowsInTableView(this);

		//if array length changed, reset from start
		if (fromIndex > numberOfCell-1)
		{
			fromIndex = 0;
			_visibleStartCellOffSetY = 0;
		}
		
		_cellHolders = new List<UITableViewCellHolder>(numberOfCell);
		for (int i = 0; i < numberOfCell; i++)
		{
			_cellHolders.Add(new UITableViewCellHolder());
		}
		
		Vector4 finalClipRegion = mPanel.finalClipRegion;
		
//		float totalHeight = finalClipRegion.w / 2;
		float totalHeight = ContentVisibleTop() + _visibleStartCellOffSetY;
		for (int i = fromIndex; i < numberOfCell; i++)
		{
			UITableViewCellHolder cellHolder = _cellHolders[i];
			
			UITableViewCell cell = DataSource.CellForRowAtIndexInTableView(this, i);
			cellHolder.Cell = cell;
			
			float cellHeight = DataSource.HeightForRowInTableView(this, i);
			cellHolder.Cell.Index = i;
			
			totalHeight -= cellHeight;
			
			float x = (finalClipRegion.z / -2) + finalClipRegion.x;
			float y = totalHeight;// + finalClipRegion.y;
//			Debug.Log (totalHeight);
			cellHolder.Cell.SetRect(x, y, finalClipRegion.z, cellHeight);
			cellHolder.Cell.SetDragTableView(this);

			SetTouchAbilityForCellHolder(cellHolder);
			SetDeletabilityForCellHolder(cellHolder);
			
			if (i == fromIndex) 
			{ 
				_visibleStartRow = i; 
				_cacheStartRow = i;

				this.DEBUG_VisibleStartRow = i;
				this.DEBUG_CacheStartRow = i;
			}
			_visibleLastRow = i;
			_cacheLastRow = i;

			this.DEBUG_VisibleLastRow = i;
			this.DEBUG_CacheLastRow = i; 
			
			if (totalHeight < ContentVisibleBottom() /*-finalClipRegion.w/2*/) { break; }
		}
		
		for (int i = _visibleStartRow; i > (_visibleStartRow-(InvisibleCellNumber/2)); i--)
		{
			GenerateCellToTopOrBottom(TopOrBottom.Top);
		}
		
		//Add cache cells
		for (int i = _visibleLastRow; i < (_visibleLastRow+(InvisibleCellNumber/2)); i++)
		{
//			AddNewCellToLast();
			GenerateCellToTopOrBottom(TopOrBottom.Bottom);
		}

	}

	protected virtual void FixedUpdate ()
	{
//		Debug.Log (mPanel.finalClipRegion.y);
//		return;

		if (_cellHolders == null || _cellHolders.Count <= 0) { return; }

		if (_lastOffSetY != mPanel.clipOffset.y)
		{
			UITableViewCellHolder firstVisibleCellHolder = _cellHolders[_visibleStartRow];
			if (ContentVisibleTop() <= firstVisibleCellHolder.Cell.Bottom)
			{
				UITableViewCellHolder firstCacheCellHolder = _cellHolders[_cacheStartRow];
				if ((firstVisibleCellHolder.Cell.Index - firstCacheCellHolder.Cell.Index) >= InvisibleCellNumber/2)
				{
//					Debug.Log ("Delete first cache cell, put it to pool");
					KillCellAtTopOrBottom(TopOrBottom.Top);
				}
				else
				{
//					Debug.Log ("No top cache cell to delete");
				}

				if (_visibleStartRow < _cellHolders.Count-1) { _visibleStartRow++; this.DEBUG_VisibleStartRow = _visibleStartRow; }
			}

			if (ContentVisibleTop() >= firstVisibleCellHolder.Cell.Top)
			{
				// add first
				UITableViewCellHolder firstCacheCellHolder = _cellHolders[_cacheStartRow];
				if (firstCacheCellHolder.Cell.Index <= 0)
				{
//					Debug.Log("No top cell to cache");
				}
				else
				{
//					Debug.Log("Add cache cell to first");
					GenerateCellToTopOrBottom(TopOrBottom.Top);
				}

				if (_visibleStartRow > _cacheStartRow) { _visibleStartRow--; this.DEBUG_VisibleStartRow = _visibleStartRow; }
//				Debug.Log ("Visible Start Row is "+_visibleStartRow);
			}


			UITableViewCellHolder lastVisibleCellHolder = _cellHolders[_visibleLastRow];
			if (ContentVisibleBottom() >= lastVisibleCellHolder.Cell.Top)
			{
				UITableViewCellHolder lastCacheCellHolder = _cellHolders[_cacheLastRow];
				if ((lastCacheCellHolder.Cell.Index - lastVisibleCellHolder.Cell.Index) >= InvisibleCellNumber/2)
				{
//					Debug.Log("Delete last cell, put it to pool");
					KillCellAtTopOrBottom(TopOrBottom.Bottom);
				}
				else
				{
//					Debug.Log("No bottom cache cell to delete");
				}

				if (_visibleLastRow > 0) { _visibleLastRow--; this.DEBUG_VisibleLastRow = _visibleLastRow; }
//				Debug.Log ("Visible Last Row is "+_visibleLastRow);
			}

			if (ContentVisibleBottom() <= lastVisibleCellHolder.Cell.Bottom)
			{
				//add last
				UITableViewCellHolder lastCacheCellHolder = _cellHolders[_cacheLastRow];
				if (lastCacheCellHolder.Cell.Index >= _cellHolders.Count-1)
				{
//					Debug.Log("No bottom cell to cache");
				}
				else
				{
//					Debug.Log("Add cache cell to last"); 
					GenerateCellToTopOrBottom(TopOrBottom.Bottom);
				}

				if (_visibleLastRow < _cacheLastRow) { _visibleLastRow++; this.DEBUG_VisibleLastRow = _visibleLastRow; }
//				Debug.Log ("Visible Last Row is "+_visibleLastRow);
			}

			_lastOffSetY = mPanel.clipOffset.y;
		}
	}

	protected float ContentVisibleTop ()
	{
//		Debug.Log (mPanel.finalClipRegion.y);
		return (/*mPanel.clipOffset.y + */(mPanel.GetViewSize().y / 2)+ mPanel.finalClipRegion.y);
	}

	protected float ContentVisibleBottom ()
	{
		return ((mPanel.GetViewSize().y / -2) + mPanel.finalClipRegion.y);
	}

	void KillCellAtTopOrBottom (TopOrBottom TorB)
	{
		UITableViewCellHolder cacheCellHolder = null;
		UITableViewCell cell = null;
		int index = 0;
		if (TorB == TopOrBottom.Top)
		{ 
			//find first cache cell
			cacheCellHolder = _cellHolders[_cacheStartRow]; 
			cell = cacheCellHolder.Cell;
			//index+1
			index = cell.Index+1;

			//delete or put it into pool
			EnqueueReusableCell(cacheCellHolder);

			//change cacheIndex
			_cacheStartRow = index;
			this.DEBUG_CacheStartRow = index;
		}
		else 
		{ 
			//find last cache cell
			cacheCellHolder = _cellHolders[_cacheLastRow];
			cell = cacheCellHolder.Cell;
			//index-1
			index = cell.Index-1;

			//delete or put it into pool
			EnqueueReusableCell(cacheCellHolder);

			//change cacheIndex
			_cacheLastRow = index;
			this.DEBUG_CacheLastRow = index;
		}
	}

	void SetDeletabilityForCellHolder (UITableViewCellHolder cellHolder)
	{
		IUITableViewEditable deletableTarget = DelegateTarget as IUITableViewEditable;
		if (deletableTarget != null)
		{
			cellHolder.Cell.onDelete += OnDeleteCellForRow;
			cellHolder.Cell.ChangeToStatus(_cellStatus);
		}
	}

	void SetTouchAbilityForCellHolder (UITableViewCellHolder cellHolder)
	{
		IUITableViewTouchable touchableTarget = DelegateTarget as IUITableViewTouchable;
		if (touchableTarget != null) 
		{ 
			cellHolder.Cell.onTouched += OnTouchedForRow;
//			cellHolder.Cell.IsTouchable = true;
		}
	}

	void GenerateCellToTopOrBottom (TopOrBottom TorB)
	{
		if (TorB == TopOrBottom.Top)
		{
			if (_cacheStartRow <= 0) { return; }
			
			UITableViewCellHolder oldCellHolder = _cellHolders[_cacheStartRow];
			int index = oldCellHolder.Cell.Index-1;
			
			UITableViewCellHolder newCellHolder = _cellHolders[index];
			
			UITableViewCell cell = DataSource.CellForRowAtIndexInTableView(this, index);
			newCellHolder.Cell = cell;
			
			float cellHeight = DataSource.HeightForRowInTableView(this, index);
			newCellHolder.Cell.Index = index;
			
			newCellHolder.Cell.SetRect(oldCellHolder.Cell.Left, oldCellHolder.Cell.Top/*+cellHeight*/, oldCellHolder.Cell.localSize.x, cellHeight);
			newCellHolder.Cell.SetDragTableView(this);

			SetTouchAbilityForCellHolder(newCellHolder);
			SetDeletabilityForCellHolder(newCellHolder);
			
			_cacheStartRow = index;
			this.DEBUG_CacheStartRow = index;
		}
		else
		{
			if (_cacheLastRow >= _cellHolders.Count-1) { return; }
			
			UITableViewCellHolder oldCellHolder = _cellHolders[_cacheLastRow];
			int index = oldCellHolder.Cell.Index+1;
			
			UITableViewCellHolder newCellHolder = _cellHolders[index];
			
			UITableViewCell cell = DataSource.CellForRowAtIndexInTableView(this, index);
			newCellHolder.Cell = cell;
			
			float cellHeight = DataSource.HeightForRowInTableView(this, index);
			newCellHolder.Cell.Index = index;
			
			newCellHolder.Cell.SetRect(oldCellHolder.Cell.Left, oldCellHolder.Cell.Bottom-cellHeight, oldCellHolder.Cell.localSize.x, cellHeight);
			newCellHolder.Cell.SetDragTableView(this);

			SetTouchAbilityForCellHolder(newCellHolder);
			SetDeletabilityForCellHolder(newCellHolder);
			
			_cacheLastRow = index;
			this.DEBUG_CacheLastRow = index;
		}
	}

	void EnqueueReusableCell (UITableViewCellHolder cellHolder)
	{
        if (cellHolder.Cell == null/* || _reusableCellQueueDictionary == null*/) { return; }

		DataSource.CellForRowWillBeKilled(this, cellHolder.Cell.Index);

		int alreadyReuseCellNum = 0;
        if (_reusableCellQueueDictionary != null)
        {
            foreach (var queue in _reusableCellQueueDictionary.Values) { alreadyReuseCellNum += queue.Count; }
        }

		//destory directly then return
		if (alreadyReuseCellNum >= MaxReusableCellNumber || cellHolder.Cell.ReuseIndentifier == null || _reusableCellQueueDictionary == null) 
		{
			DestoryCellWithoutCache(cellHolder); 
			return;  
		}

		//Add to queue
		bool isHasCachePool = _reusableCellQueueDictionary.ContainsKey(cellHolder.Cell.ReuseIndentifier);
		Queue<UITableViewCell> cachePool = null;
		if (!isHasCachePool) 
		{
			cachePool = new Queue<UITableViewCell>();
			_reusableCellQueueDictionary.Add(cellHolder.Cell.ReuseIndentifier, cachePool);
		}
		else
		{
			cachePool = _reusableCellQueueDictionary[cellHolder.Cell.ReuseIndentifier];
		}
		cachePool.Enqueue(cellHolder.Cell);
		cellHolder.Cell.Index = UITableViewCell.DEFAULT_INDEX;
		cellHolder.Cell.onTouched = null;
		cellHolder.Cell.onDelete = null;
		cellHolder.Cell.IsDeletable = true;
//		cellHolder.Cell.IsTouchable = false;
		cellHolder.Cell.gameObject.SetActive(false);
		cellHolder.Cell = null;
	}

	void DestoryCellWithoutCache (UITableViewCellHolder cellHolder)
	{
		cellHolder.Cell.onTouched = null;
		cellHolder.Cell.onDelete = null;
		cellHolder.Cell.IsDeletable = true;
//		cellHolder.Cell.IsTouchable = false;
		UITableViewCell cell = cellHolder.Cell;
		cellHolder.Cell = null;
		NGUITools.Destroy(cell.gameObject);
	}

	void OnTouchedForRow (int rowIndex)
	{
		IUITableViewTouchable touchableTarget = DelegateTarget as IUITableViewTouchable;
		if (touchableTarget == null || rowIndex < _visibleStartRow || rowIndex > _visibleLastRow) { return; }
		else
		{
			Debug.Log("cell"+rowIndex+"was touched.");
			touchableTarget.DidSelectedRowAtIndexInTableView(this, rowIndex);
		}
	}

	/// <summary>
	/// Create table view cell prefab and attach it to table view with reuseIndentifier.
	/// </summary>
	/// <returns>The cell with reuse indentifier.</returns>
	/// <param name="tableViewCell">Cell prefab.</param>
	/// <param name="reuseIndentifier">Reuse indentifier.</param>
	public UITableViewCell AttachCellWithReuseIndentifier (GameObject cellPrefab, string reuseIndentifier)
	{
		GameObject cellGameObject = NGUITools.AddChild(gameObject, cellPrefab);
		if (!cellGameObject.activeSelf) { cellGameObject.SetActive(true); }

		UITableViewCell cell = cellGameObject.GetComponent<UITableViewCell>();

		string reuseIndentifier_copy = null;
		if (reuseIndentifier != null)
		{
			reuseIndentifier_copy = String.Copy(reuseIndentifier);
		}
		else
		{
			return cell;
		}

		cell.ReuseIndentifier = reuseIndentifier_copy;

		if (_reusableCellQueueDictionary == null)
		{
			_reusableCellQueueDictionary = new Dictionary<string, Queue<UITableViewCell>>();
		}

//		if (reuseIndentifier_copy == null) { return cell; }

		bool isHasCachePool = _reusableCellQueueDictionary.ContainsKey(reuseIndentifier_copy);
		if (!isHasCachePool)
		{
			Queue<UITableViewCell> cachePool = new Queue<UITableViewCell>();
			_reusableCellQueueDictionary.Add(reuseIndentifier_copy, cachePool);
		}

		return cell;
	}

	/// <summary>
	/// Search the reusable cell from cache queue.
	/// Return cell if found or return null if not found.
	/// </summary>
	/// <returns>The reusable cell with indentifier.</returns>
	/// <param name="indentifier">Indentifier.</param>
	public UITableViewCell DequeueReusableCellWithIndentifier (string indentifier)
	{
		string indentifier_copy = String.Copy(indentifier);
		if (_reusableCellQueueDictionary == null || indentifier_copy == null) { return null; }

		bool isHasCacheQueue = _reusableCellQueueDictionary.ContainsKey(indentifier_copy);
		if (!isHasCacheQueue) { return null; }

		Queue<UITableViewCell> cacheQueue = _reusableCellQueueDictionary[indentifier_copy];
		if (cacheQueue.Count <= 0) { return null; }

		UITableViewCell cacheCell = cacheQueue.Dequeue();
		cacheCell.gameObject.SetActive(true);
//		Debug.Log("Get reusable cell.................");
		return cacheCell;
	}

	public void ReloadData ()
	{
        ResetData();

		ReloadDataFromRowIndex(_visibleStartRow);
		_invisibleBounds = InvisibleBounds();

	}

    /// <summary>
    /// Cells where is appearing on screen or return null when out of screen.
    /// </summary>
    /// <returns>The for row.</returns>
    /// <param name="rowIndex">Row index.</param>
    public UITableViewCell CellForRow (int rowIndex)
    {
        if (_cellHolders == null) { return null; }

        foreach (var cellHolder in _cellHolders)
        {
            if (cellHolder.Cell != null && cellHolder.Cell.Index == rowIndex)
            {
                return cellHolder.Cell;
            }
        }

        return null;
    }

//	private float _willDeleteCellTopOffsetY = 0;
	private float _willDeleteCellHeight = 0;
//	private GameObject _fakeCellGameObject = null;
	void OnDeleteCellForRow (int index)
	{
		IUITableViewEditable editableTarget = DelegateTarget as IUITableViewEditable;
		if (editableTarget == null || index > _cellHolders.Count-1) { return; }

		//Remeber the position of cell
//		_willDeleteCellTopOffsetY = _cellHolders[index].Cell.Top;
		UITableViewCellHolder willDeleteCellHolder = _cellHolders[index];
		_willDeleteCellHeight = willDeleteCellHolder.Cell.localSize.y;
		GameObject cellGameObject = willDeleteCellHolder.Cell.gameObject;

//		GameObject fakeGameObject = null;
		editableTarget.WillDeleteRowAtIndexInTableView(this, index, cellGameObject);

	}

//	public void ConfirmDeleteCellForRowAtIndex (int rowIndex, /GameObject fakeGameObject, IEnumerator deleteAnimation)
	public void ConfirmDeleteCellForRowAtIndex (int rowIndex, IEnumerator deleteAnimation)
	{
//		UITableViewCellHolder willDeleteCellHolder = _cellHolders[rowIndex];
//		fakeGameObject = willDeleteCellHolder.Cell.gameObject;

		float totalHeight = 0;
		for (int i = _cacheLastRow+1; i < _cellHolders.Count; i++)
		{
			UITableViewCellHolder oldCellHolder = _cellHolders[_cacheLastRow];
			UITableViewCellHolder newCellHolder = _cellHolders[i];
			
			UITableViewCell cell = DataSource.CellForRowAtIndexInTableView(this, i);
			newCellHolder.Cell = cell;
			
			float cellHeight = DataSource.HeightForRowInTableView(this, i);
			newCellHolder.Cell.Index = i;
			
			newCellHolder.Cell.SetRect(oldCellHolder.Cell.Left, oldCellHolder.Cell.Bottom-cellHeight, oldCellHolder.Cell.localSize.x, cellHeight);
			newCellHolder.Cell.SetDragTableView(this);
			
			SetTouchAbilityForCellHolder(newCellHolder);
			SetDeletabilityForCellHolder(newCellHolder);
			
			_cacheLastRow = i;
			_visibleLastRow++;

			totalHeight += cellHeight;
			if (totalHeight >= _willDeleteCellHeight) { break; }
		}

		this.DEBUG_CacheLastRow = _cacheLastRow;
		this.DEBUG_VisibleLastRow = _visibleLastRow;

		StartCoroutine(PlayDeleteCellAnimation(deleteAnimation, rowIndex));
	}

	IEnumerator PlayDeleteCellAnimation (IEnumerator deleteAnimation, int deleteRowIndex)
	{
		//Disable tableview while playing animate.
		this.enabled = false;

		//Diable all cell delete button
		foreach (var cellHolder in _cellHolders)
		{
			if (cellHolder.Cell != null)
			{
				cellHolder.Cell.IsDeletable = false;
			}
		}

		yield return StartCoroutine(deleteAnimation);

		UITableViewCellHolder deleteCellHolder = _cellHolders[deleteRowIndex];
		EnqueueReusableCell(deleteCellHolder);

		int cacheLastRow = _cacheLastRow;
		for (int i = deleteRowIndex+1; i <= cacheLastRow; i++)
		{
			UITableViewCellHolder cellHolder = _cellHolders[i];
			Vector3 position = cellHolder.Cell.gameObject.transform.localPosition;

			//Without play animation
//			cellHolder.Cell.gameObject.transform.localPosition = new Vector3(position.x, position.y + _willDeleteCellHeight);
			cellHolder.Cell.Index = i-1;

			//Play moving animation
			TweenPosition.Begin(cellHolder.Cell.gameObject, 0.2f, new Vector3(position.x, position.y + _willDeleteCellHeight));
			yield return new WaitForFixedUpdate();
		}

		yield return new WaitForSeconds(0.3f);

		_cellHolders.Remove(deleteCellHolder);
		_cacheLastRow--;
		_visibleLastRow--;
		this.DEBUG_CacheLastRow = _cacheLastRow;
		this.DEBUG_VisibleLastRow = _visibleLastRow;
		Debug.Log("Cell Holder num = "+_cellHolders.Count);

		IUITableViewEditable editableTarget = DelegateTarget as IUITableViewEditable;
		editableTarget.DidDeleteRowAtIndexInTableView(this, deleteRowIndex);

		//Enable tableView scrollability after animation end.
		this.enabled = true;

		//Enable all cell delete buttons
		foreach (var cellHolder in _cellHolders)
		{
			if (cellHolder.Cell != null)
			{
				cellHolder.Cell.IsDeletable = true;
			}
		}
	}

	public void EnterEditingMode (bool isEditing)
	{
		if ((isEditing && _cellStatus == UITableViewCellStatus.Deletable) || 
		    (!isEditing && _cellStatus == UITableViewCellStatus.Normal))
		{
			Debug.Log("The Editing mode is already "+isEditing);
			return;
		}

		if (isEditing) { _cellStatus = UITableViewCellStatus.Deletable; }
		else { _cellStatus = UITableViewCellStatus.Normal; }

		foreach (var cellHolder in _cellHolders)
		{
			if (cellHolder.Cell != null)
			{
				cellHolder.Cell.ChangeToStatus(_cellStatus);
			}
		}
	}

	#region ScrollBar method
	protected virtual Bounds InvisibleBounds ()
	{
		Bounds b = new Bounds();

		float contentHeight = 0;
		int count = _cellHolders.Count;
		for (int rowIndex = 0; rowIndex < count; rowIndex++)
		{
			contentHeight += DataSource.HeightForRowInTableView(this, rowIndex);
		}

		float contentTop = mPanel.GetViewSize().y;
		float contentBottom = contentTop - contentHeight;

		Vector3 bmax = new Vector3(mBounds.min.x, contentTop);
		Vector3 bmin = new Vector3(mBounds.max.x, contentBottom);

		b.SetMinMax(bmin, bmax);

//		Debug.Log("contentTop: "+contentTop+"  contentBottom: "+contentBottom);

		return b;
	}

	public override void UpdateScrollbars (bool recalculateBounds)
	{
		if (mPanel == null) return;
		
		if (horizontalScrollBar != null || verticalScrollBar != null)
		{
			if (recalculateBounds)
			{
				mCalculatedBounds = false;
				mShouldMove = shouldMove;
			}
			
//			Bounds b = InvisibleBounds();
			Vector2 bmin = _invisibleBounds.min;
			Vector2 bmax = _invisibleBounds.max;

			if (verticalScrollBar != null && bmax.y > bmin.y)
			{
				Vector4 clip = mPanel.finalClipRegion;
				int intViewSize = Mathf.RoundToInt(clip.w);
				if ((intViewSize & 1) != 0) intViewSize -= 1;
				float halfViewSize = intViewSize * 0.5f;
				halfViewSize = Mathf.Round(halfViewSize);
				
				if (mPanel.clipping == UIDrawCall.Clipping.SoftClip)
					halfViewSize -= mPanel.clipSoftness.y;
				
				float contentSize = bmax.y - bmin.y;
				float viewSize = halfViewSize * 2f;
				float contentMin = bmin.y;
				float contentMax = bmax.y;
				float viewMin = clip.y - halfViewSize;
				float viewMax = clip.y + halfViewSize;
				
				contentMin = viewMin - contentMin;
				contentMax = contentMax - viewMax;
				
				UpdateScrollbars(verticalScrollBar, contentMin, contentMax, contentSize, viewSize, true);
			}
		}
		else if (recalculateBounds)
		{
			mCalculatedBounds = false;
		}
	}

	#endregion

}
