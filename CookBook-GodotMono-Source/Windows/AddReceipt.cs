//this project uses JSON.NET from Newtonsoft so had to add the package to this project
//the data folder need to be copied manually when exporting
//author: gametemplates.itch.io

using Godot;
using System;
using Newtonsoft.Json;
using System.IO;
using System.Text;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Collections.Generic;

public class AddReceipt : Node
{
    OptionButton volumeOption;
	OptionButton ingredientOption;
	LineEdit amountOption;
    TabContainer tabContainer;
	ItemList ingredientList;
	LineEdit receiptTitle;
	TextEdit receiptDescription;
	PopupMenu ingredientsPopup;
	WindowDialog confirmationPopupDialog;
	Label titleLabel;
	Label receiptLabel;
	Label ingredientsLabel;
    Button ingredientAddButton;
    Button saveButton;
	Button deleteButton;

	string ingredientPath;
	string volumePath;
	string receiptPath;
	
	StreamReader r;
	string json;
	JObject rss;
	string configurationPath;
	
	//these strings going to store the messages we display in the popup dialog
	string receiptAlreadyExistMsg;
	string ingredientAlreadyExistMsg;
	string receiptSavedToJsonMsg;

    //string to store text to display as title in the file dialog window 
    string fileDialogTitle;

	List<Ingredient> IngredientList = new List<Ingredient>(); //using this to store current list of ingredients so we can query the list easily and write it in to JSON

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        //get reference to data files
		ingredientPath = Options.IngredientPath;
		volumePath = Options.VolumePath;
		receiptPath = Options.ReceiptPath;
		configurationPath = Options.ConfigurationPath;
		
        //get reference to controls
        volumeOption = (OptionButton)GetNode("VolumeOption");
		ingredientOption = (OptionButton)GetNode("IngredientOption");
		amountOption = (LineEdit)GetNode("AmountOption");
		ingredientList = (ItemList)GetNode("Ingredients");
        receiptTitle = (LineEdit)GetNode("Title");
        receiptDescription = (TextEdit)GetNode("Receipt");
        tabContainer = (TabContainer)GetParent();
		ingredientsPopup = (PopupMenu)GetNode("Ingredients").GetNode("PopupMenu");
		confirmationPopupDialog = (WindowDialog)GetParent().GetParent().GetNode("ConfirmationWindowDialog");
        titleLabel = (Label)GetNode("TitleLabel");
        receiptLabel = (Label)GetNode("ReceiptLabel");
        ingredientsLabel = (Label)GetNode("IngredientsLabel");
        ingredientAddButton = (Button)GetNode("AddButton");
        saveButton = (Button)GetNode("SaveButton");
		deleteButton = (Button)GetNode("Ingredients").GetNode("PopupMenu").GetNode("DeleteButton");;
		
		//get language configuration from JSON
		r = new StreamReader(configurationPath);
		json = r.ReadToEnd();
		r.Close();
		
		rss = JObject.Parse(json); //convert string to object
        string _language = rss["Language"].ToString();
        
		//now get the language file
		r = new StreamReader("data/languages/lang.json");
		json = r.ReadToEnd();
		r.Close();
		
		rss = JObject.Parse(json);
		string _langFile = rss[_language].ToString();
		
		//now get all the text for the selected language
		r = new StreamReader($"data/languages/{_langFile}.json");
		json = r.ReadToEnd();
		r.Close();	
		rss = JObject.Parse(json);
		
		//now change text on all buttons and labels
		this.SetName(rss["New Receipt"].ToString());
		ingredientsLabel.Text = rss["Ingredients"].ToString();
		titleLabel.Text = rss["Title"].ToString();
        receiptLabel.Text = rss["Description"].ToString();
        ingredientAddButton.Text = rss["Add"].ToString();
        saveButton.Text = rss["Save"].ToString();
		deleteButton.Text = rss["Delete"].ToString();
		
		//now get messages we are going to display in popup dialog
		receiptAlreadyExistMsg = rss["ReceiptAlreadyExistMsg"].ToString();
		ingredientAlreadyExistMsg = rss["IngredientAlreadyExistMsg"].ToString();
		receiptSavedToJsonMsg = rss["ReceiptSavedToJsonMsg"].ToString();

        //get file dialog title

        
    }

 // Called every frame. 'delta' is the elapsed time since the previous frame.
  public override void _Process(float delta)
  {
        //if left mouse button is clicked anywhere and the ingredients popup is shown, hide it
        if(Input.IsActionPressed("left_mouse_button"))
        {
            if(ingredientsPopup.IsVisible())
            {
                ingredientsPopup.Hide();
            }
        }
    }

	private void _on_AddIngredientButton_button_up()
	{
	    //when add button is clicked, add ingredient to list
		var _amount = amountOption.GetText();
        var _volume = volumeOption.GetText();
		var _ingredient = ingredientOption.GetText();
		
		bool _match = false;
		for(var i = 0; i < IngredientList.Count; i++)
		{
			if(IngredientList[i].Ingredients == _ingredient)
				_match = true;
		}
		
		if(_match != true && _amount != "0" && _amount != "" && _amount != " ")
		{
			ingredientList.AddItem($"{_amount} {_volume} {_ingredient}");
	        IngredientList.Add(new Ingredient(_amount, _volume, _ingredient));
		}
		else
		{
			//show dialog, ingredient already exist.
			_displayMessage(ingredientAlreadyExistMsg);
		}
	}
	
	private void _on_TabContainer_tab_selected(int tab)
	{
    	//if add receipt tab button is selected, update the ingredient and volume options
		if(tab == 1)
		{
            //cleal the options first
            ingredientOption.Clear();
            volumeOption.Clear();
			
			//read list of ingredients from JSON and add it to the option button
            try
            {
                StreamReader r = new StreamReader(ingredientPath);
                string _json = r.ReadToEnd();
                r.Close();

                JObject rss = JObject.Parse(_json); //convert string to object
                var _list = rss["Ingredients"].ToList(); //convert to list using Linq
                for (var l = 0; l < _list.Count; l++) //add each item to list
                {
                    ingredientOption.AddItem(rss["Ingredients"][l].ToString());
                }
            }
            catch (FileNotFoundException)
            {
                //do nothing, failed to load file...
            }

            //read list of volumes from JSON and add it to the option button
            try
            {
                StreamReader r = new StreamReader(volumePath);
                string _json = r.ReadToEnd();
                r.Close();

                JObject rss = JObject.Parse(_json); //convert string to object
                var list = rss["Volumes"].ToList(); //convert to list using Linq
                for (var l = 0; l < list.Count; l++) //add each item to list
                {
                    volumeOption.AddItem(rss["Volumes"][l].ToString());
                }
            }
            catch (FileNotFoundException)
            {
                //do nothing, failed to load file...
            }
		}
	}
	
	private void _on_SaveButton_button_up()
	{
        //Label _popupMessage; //going to use to get label of popup dialog so we can change the text displayed in the popup
        
        //get title and description of receipt
    	var _title = receiptTitle.Text;
		var _description = receiptDescription.Text;

        if (_title != "" && _title != " " && _description != "" && _description != " " && ingredientList.GetItemCount() != 0)
        {
            //attempt to read existing receipts from JSON
            try
            {
                StreamReader _r = new StreamReader(receiptPath);
                string _json = _r.ReadToEnd();
                _r.Close();

                JObject _jsonObject = JObject.Parse(_json); //convert string to object
                JArray _currentReceipts = _jsonObject["Receipts"].Value<JArray>();
                JObject _newReceipts = new JObject();
				
				//check if the title of the current receipt already exist in json
				bool _match = false;
				for(var t = 0; t < _currentReceipts.Count; t++)
                {
                    if(_title.ToLower() == _currentReceipts[t]["Title"].ToString().ToLower())
                    {
                        //display message receipt with the same title already exist
						_displayMessage(receiptAlreadyExistMsg);
                        _match = true;
                    }
                }

                //if no receipt with same title is found, attempt to add the new receipt to the existing receipts in json
				if(!_match)
				{
	                StringBuilder _sb = new StringBuilder();
	                StringWriter _sw = new StringWriter(_sb);
	                StreamWriter _w = new StreamWriter(receiptPath);
	
	                using (JsonWriter _writer = new JsonTextWriter(_sw))
	                {
	                    _writer.Formatting = Formatting.Indented;
	                    _writer.WriteStartObject();
	                    _writer.WritePropertyName("Title");
	                    _writer.WriteValue(_title);
	                    _writer.WritePropertyName("Description");
	                    _writer.WriteValue(_description);
	                    _writer.WritePropertyName("Ingredients");
	                    _writer.WriteStartArray();
	                    for (var i = 0; i < ingredientList.GetItemCount(); i++)
	                    {
	                        _writer.WriteStartObject();
	                        _writer.WritePropertyName("Amount");
	                        _writer.WriteValue(IngredientList[i].Amount);
	                        _writer.WritePropertyName("Volume");
	                        _writer.WriteValue(IngredientList[i].Volume);
	                        _writer.WritePropertyName("Ingredient");
	                        _writer.WriteValue(IngredientList[i].Ingredients);
	                        _writer.WriteEndObject();
	                    }
	                    _writer.WriteEndArray();
	                    _writer.WriteEndObject();
	
	                    _currentReceipts.Add(JObject.Parse(_sw.ToString()));
	                    _newReceipts["Receipts"] = _currentReceipts;
	
	                    //write the result in to JSON
	                    _w.Write(_newReceipts);
	                    _w.Close();
	
	                }
					
					//show dialog, save was successfull.
					_displayMessage(receiptSavedToJsonMsg);
					
					//clear the form for the next receipt we may want to add
        			receiptTitle.Text = "";
        			receiptDescription.Text = "";
        			ingredientList.Clear();
        			IngredientList.Clear();
				}

            }
            catch (FileNotFoundException)
            {
                //if JSON file containing the receipts is not found, write current receipt in to a new JSON
                StringBuilder _sb = new StringBuilder();
                StringWriter _sw = new StringWriter(_sb);
                StreamWriter _w = new StreamWriter(receiptPath);

                using (JsonWriter _writer = new JsonTextWriter(_sw))
                {
                    _writer.Formatting = Formatting.Indented;

                    _writer.WriteStartObject();
                    _writer.WritePropertyName("Receipts");
                    _writer.WriteStartArray();
                    _writer.WriteStartObject();
                    _writer.WritePropertyName("Title");
                    _writer.WriteValue(_title);
                    _writer.WritePropertyName("Description");
                    _writer.WriteValue(_description);
                    _writer.WritePropertyName("Ingredients");
                    _writer.WriteStartArray();
                    for (var i = 0; i < ingredientList.GetItemCount(); i++)
                    {
                        _writer.WriteStartObject();
                        _writer.WritePropertyName("Amount");
                        _writer.WriteValue(IngredientList[i].Amount);
                        _writer.WritePropertyName("Volume");
                        _writer.WriteValue(IngredientList[i].Volume);
                        _writer.WritePropertyName("Ingredient");
                        _writer.WriteValue(IngredientList[i].Ingredients);
                        _writer.WriteEndObject();
                    }
                    _writer.WriteEndArray();
                    _writer.WriteEndObject();
                    _writer.WriteEndArray();
                    _writer.WriteEndObject();

                    _w.Write(_sw);
                    _w.Close();
                }
				
				//clear the form for the next receipt we may want to add
        		receiptTitle.Text = "";
        		receiptDescription.Text = "";
        		ingredientList.Clear();
        		IngredientList.Clear();

            }
        }

	}
	
	private void _on_Ingredients_item_rmb_selected(int _index, Vector2 _at_position)
	{
    	//if item is selected with right mouse button, display popup menu
		ingredientsPopup.Show();

        //set position of popup menu to the position of the item
        ingredientsPopup.SetGlobalPosition(GetViewport().GetMousePosition());
	}
	
	private void _on_IngredientsDeleteButton_pressed()
	{
        //if ingredients popup delete button is pressed, delete selected item
        var id = ingredientList.GetSelectedItems();
        ingredientList.RemoveItem(id[0]);
		IngredientList.RemoveAt(id[0]);		
	}
	
	private void _displayMessage(string _msg) //method to display a popup message
	{
		Label _popupMessage = (Label)confirmationPopupDialog.GetNode("MessageLabel");
		_popupMessage.Text = _msg;
        confirmationPopupDialog.Show();
	}
		
}

