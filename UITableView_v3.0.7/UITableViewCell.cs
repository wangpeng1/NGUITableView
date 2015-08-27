/// <summary>
/// Compatiable with NGUI 3.0.6 f7
/// zhao-zilong 
/// </summary>

using UnityEngine;
using System.Collections;
using System.Linq;

public enum UITableViewCellStatus
{
	Normal,
	Deletable,
} 

[ExecuteInEditMode]
[RequireComponent(typeof(UIDragScrollView))]
[RequireComponent(typeof(BoxCollider))]
[AddComponentMenu("NGUI/Interaction/Table View Cell")]
public class UITableViewCell : UIWidget 
{
	public static int DEFAULT_INDEX = -1;
	private int _index = DEFAULT_INDEX;
	private bool _isTouchable = true;
	private bool _isDeletable = false;
	private string _reuseIndentifier = null;


	private UITableCellDeleteButton _deleteButton = null;
	
	/// <summary>
	/// Gets or sets the cell's index.
	/// </summary>
	/// <value>The index.</value>
	public int Index 
	{ 
		get
		{
			return _index;
		}
		set
		{
			_index = value;

			//Just for test. Better to delete under release mode.
			gameObject.name = _index.ToString();
		}
	}

	/// <summary>
	/// Gets or sets the reuse indentifier.
	/// Different cell should use different Indentifier.
	/// </summary>
	/// <value>The reuse indentifier.</value>
	public string ReuseIndentifier 
	{
		get 
		{
			return _reuseIndentifier;
		}
		set
		{
			_reuseIndentifier = value;
		}
	}

	public bool IsDeletable
	{
		get
		{
			return _isDeletable;
		}
		set
		{
			_isDeletable = value;

			if (_deleteButton != null)
			{
				if (_isDeletable)
				{
					_deleteButton.isEnabled = true;
				}
				else
				{
					_deleteButton.isEnabled = false;
				}

			}
		}
	}

	public float Top
	{
		get
		{
			return gameObject.transform.localPosition.y;
		}
	}

	public float Bottom
	{
		get
		{
			return gameObject.transform.localPosition.y - localSize.y;
		}
	}

	public float Left
	{
		get
		{
			return gameObject.transform.localPosition.x - (localSize.x / 2);
		}
	}

	public delegate void OnTouchedNotification(int index);
	public OnTouchedNotification onTouched;

	public delegate void OnDeleteNotification(int index);
	public OnDeleteNotification onDelete;

	protected override void Awake ()
	{
		base.Awake ();

		gameObject.GetComponent<BoxCollider>().enabled = true;
		autoResizeBoxCollider = true;

		_deleteButton =  gameObject.transform.GetComponentsInChildren<UITableCellDeleteButton>(true).FirstOrDefault();

		if (_deleteButton != null)
		{
			UIEventListener.Get(_deleteButton.gameObject).onClick = onDeleteButtonClick;
			_deleteButton.gameObject.SetActive(false);
		}
	}

	void OnClick ()
	{
		if (_isTouchable == true && onTouched != null)
		{
			onTouched(_index);
		}
	}

	void onDeleteButtonClick (GameObject go)
	{
		if (onDelete != null)
		{
			onDelete(_index);
		}
	}

	public void SetDragTableView (UITableView tableView)
	{
		gameObject.GetComponent<UIDragScrollView>().scrollView = tableView;
	}

	public void ChangeToStatus (UITableViewCellStatus status)
	{
		switch (status)
		{
		case UITableViewCellStatus.Normal:
			_isTouchable = true;
			if (_deleteButton != null)
			{
				_deleteButton.gameObject.SetActive(false);
			}
			break;
		case UITableViewCellStatus.Deletable:
			_isTouchable = false;
			if (_deleteButton != null)
			{
				_deleteButton.gameObject.SetActive(true);
			}
			break;
		}
	}

	public void SetRect (float x, float y, float width, float height)
	{
		Vector2 po = pivotOffset;

		float fx = Mathf.Lerp(x, x + width, po.x);
		float fy = Mathf.Lerp(y, y + height, po.y);

		int finalWidth = Mathf.FloorToInt(width + 0.5f);
		int finalHeight = Mathf.FloorToInt(height + 0.5f);

		if (po.x == 0.5f) finalWidth = ((finalWidth >> 1) << 1);
		if (po.y == 0.5f) finalHeight = ((finalHeight >> 1) << 1);

		Transform t = cachedTransform;
		Vector3 pos = t.localPosition;
		pos.x = Mathf.Floor(fx + 0.5f);
		pos.y = Mathf.Floor(fy + 0.5f);

		if (finalWidth < minWidth) finalWidth = minWidth;
		if (finalHeight < minHeight) finalHeight = minHeight;

		t.localPosition = pos;
		this.width = finalWidth;
		this.height = finalHeight;
	}
}
