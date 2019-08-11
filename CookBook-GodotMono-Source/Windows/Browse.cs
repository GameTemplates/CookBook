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
using System.Drawing.Printing;

public class Browse : Tabs
{
    ItemList receiptList;
    ItemList receiptIngredients;
    LineEdit receiptTitle;
    TextEdit receiptDescription;
	PopupMenu ingredientsPopup;
	LineEdit ingredientAmountOption;
	OptionButton ingredientVolumeOption;
	OptionButton ingredientOption;
    FileDialog saveFileDialog;
    LineEdit receiptSearch;
    Label receiptsLabel;
    Label searchLabel;
    Label titleLabel;
    Label descriptionLabel;
    Label ingredientsLabel;
    Button saveButton;
    Button printButton;
    Button updateButton;
	Button deleteButton;
    Button ingredientAddButton;
    Button ingredientSearchAddButton;
	
	OptionButton ingredientSearchOption;
	ItemList ingredientSearchList;
	PopupMenu ingredientSearchPopup;
	WindowDialog confirmationPopupDialog;

    string receiptPath;
	string ingredientPath;
	string volumePath;
	int ingredientSearchListCount;
	string initialTitle; //we use this variable to store the initial title of a receipt when selected to check later if the title has been changed

    StreamReader r;
    string json;
    JObject rss;
    string configurationPath;

	//these strings going to store the messages we display in the popup dialog
	string receiptAlreadyExistMsg;
	string ingredientAlreadyExistMsg;
	string receiptSavedToJsonMsg;
	string receiptSavedToFileMsg;
	string receiptUpdatedMsg;

    List<Newtonsoft.Json.Linq.JToken> list;
	List<Ingredient> IngredientList = new List<Ingredient>(); //using this to store current list of ingredients so we can query the list easily and update the JSON

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
		
        //get reference to controls
		receiptList = (ItemList)GetNode("ReceiptList");
        receiptIngredients = (ItemList)GetNode("ReceiptIngredients");
        receiptTitle = (LineEdit)GetNode("ReceiptTitle");
        receiptDescription = (TextEdit)GetNode("ReceiptDescription");
		ingredientsPopup = (PopupMenu)GetNode("ReceiptIngredients").GetNode("PopupMenu");
		ingredientAmountOption = (LineEdit)GetNode("AmountOption");
		ingredientVolumeOption = (OptionButton)GetNode("VolumeOption");
		ingredientOption = (OptionButton)GetNode("IngredientOption");
        saveFileDialog = (FileDialog)GetNode("SaveButton").GetNode("FileDialog");
        receiptSearch = (LineEdit)GetNode("Search");
        receiptsLabel = (Label)GetNode("ReceiptsLabel");
        searchLabel = (Label)GetNode("SearchLabel");
        titleLabel = (Label)GetNode("TitleLabel");
        descriptionLabel = (Label)GetNode("DescriptionLabel");
        ingredientsLabel = (Label)GetNode("IngredientsLabel");
        ingredientSearchAddButton = (Button)GetNode("IngredientSearchAddButton");
        ingredientAddButton = (Button)GetNode("AddIngredientButton");
        saveButton = (Button)GetNode("SaveButton");
        printButton = (Button)GetNode("PrintButton");
        updateButton = (Button)GetNode("UpdateButton");
		deleteButton = (Button)GetNode("ReceiptIngredients").GetNode("PopupMenu").GetNode("DeleteButton");
		
		ingredientSearchOption = (OptionButton)GetNode("IngredientSearchOption");
		ingredientSearchList = (ItemList)GetNode("IngredientSearchList");
		ingredientSearchPopup = (PopupMenu)GetNode("IngredientSearchList").GetNode("PopupMenu");
		confirmationPopupDialog = (WindowDialog)GetParent().GetParent().GetNode("ConfirmationWindowDialog");

        //get reference to the file store the receipts
        receiptPath = Options.ReceiptPath;
		ingredientPath = Options.IngredientPath;
		volumePath = Options.VolumePath;
        configurationPath = Options.ConfigurationPath;

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
        this.SetName(rss["Browse"].ToString());
        ingredientsLabel.Text = rss["Ingredients"].ToString();
        titleLabel.Text = rss["Title"].ToString();
        descriptionLabel.Text = rss["Description"].ToString();
        ingredientAddButton.Text = rss["Add"].ToString();
        ingredientSearchAddButton.Text = rss["Add"].ToString();
        saveButton.Text = rss["Save"].ToString();
        updateButton.Text = rss["Update"].ToString();
        printButton.Text = rss["Print"].ToString();
		deleteButton.Text = rss["Delete"].ToString();
        searchLabel.Text = rss["Search"].ToString();
        receiptSearch.HintTooltip = rss["Search"].ToString();
        receiptSearch.PlaceholderText = rss["Search"].ToString();
		receiptsLabel.Text = rss["Receipts"].ToString();
		saveFileDialog.SetTitle(rss["Select folder"].ToString());
		
		//now get messages we are going to display in popup dialog
		receiptAlreadyExistMsg = rss["ReceiptAlreadyExistMsg"].ToString();
		ingredientAlreadyExistMsg = rss["IngredientAlreadyExistMsg"].ToString();
		receiptSavedToJsonMsg = rss["ReceiptSavedToJsonMsg"].ToString();
		receiptSavedToFileMsg = rss["ReceiptSavedToFileMsg"].ToString();
		receiptUpdatedMsg = rss["ReceiptUpdatedMsg"].ToString();


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
				
				if(ingredientSearchPopup.IsVisible())
				{
					ingredientSearchPopup.Hide();
				}
	        }
			
			//if the number of items in ingredient search list is changed, update receipt list using the ingredient list
			if(ingredientSearchListCount != ingredientSearchList.GetItemCount())
			{
				//update the receipt list to display only those receipts that has ANY of the ingredients from the search list
	            receiptList.Clear();//clear the receipt list and search box first
	            receiptSearch.Text = "";
				
				//if the list is not empty
				if(ingredientSearchList.GetItemCount() > 0)
				{
					//go through each receipts and check which ones got any of the ingredients we search for
		            for (var r = 0; r < list.Count; r++)
		            {
						var _ingredients = list[r]["Ingredients"].ToList();
		                for(var i = 0; i < _ingredients.Count(); i++)
		                {
		                    for(var isl = 0; isl < ingredientSearchList.GetItemCount(); isl++) 
		                    {
		                        if (_ingredients[i]["Ingredient"].ToString() == ingredientSearchList.GetItemText(isl))
		                        {
									bool _receiptMatch = false;
									for(var l = 0; l < receiptList.GetItemCount(); l++)
									{
										if(receiptList.GetItemText(l) == list[r]["Title"].ToString())
											_receiptMatch = true;
									}
									
									if(_receiptMatch != true)
		                    			receiptList.AddItem(list[r]["Title"].ToString());		
		                        }
		                    }
		                }
					}
				}
				else //if item list is empty, add each receipt title to list
				{
					for (var l = 0; l < list.Count; l++)
            		{
                		receiptList.AddItem(list[l]["Title"].ToString());
            		}
				}
	      	}
				
			ingredientSearchListCount = ingredientSearchList.GetItemCount(); //update the current number of items so we know if it change later
	}      
	  

    private void _on_TabContainer_tab_selected(int _tab)
    {
        //if Browse tab is selected, load all receipts in to the receipt, volume and ingredient list
        if (_tab == 2)
        {
			
            //clear the lists and reset values
            receiptList.Clear();
			ingredientOption.Clear();
            ingredientVolumeOption.Clear();
			ingredientSearchOption.Clear();
            ingredientSearchList.Clear();
			receiptSearch.Text = "";
			ingredientSearchListCount = ingredientSearchList.GetItemCount();
            _resetForm();

            //read list of receipts from JSON and add their titles to the receipt list
            try
            {
                StreamReader _r = new StreamReader(receiptPath);
                string _json = _r.ReadToEnd();
                _r.Close();

                JObject _rss = JObject.Parse(_json); //convert string to object
                list = _rss["Receipts"].ToList(); //convert to list using Linq
                for (var l = 0; l < list.Count; l++) //add each item to list
                {
                    receiptList.AddItem(list[l]["Title"].ToString());
                }

                //sort list alphabetically
                receiptList.SortItemsByText();
            }
            catch (FileNotFoundException)
            {
                //do nothing, 
            }
			
			//read list of ingredients from JSON and add it to the option button
            try
            {
                StreamReader r = new StreamReader(ingredientPath);
                string _json = r.ReadToEnd();
                r.Close();

                JObject _rss = JObject.Parse(_json); //convert string to object
                var _list = _rss["Ingredients"].ToList(); //convert to list using Linq
                for (var l = 0; l < _list.Count; l++) //add each item to list
                {
                    ingredientOption.AddItem(_rss["Ingredients"][l].ToString());
					ingredientSearchOption.AddItem(_rss["Ingredients"][l].ToString());
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
                var _list = rss["Volumes"].ToList(); //convert to list using Linq
                for (var l = 0; l < _list.Count; l++) //add each item to list
                {
                    ingredientVolumeOption.AddItem(rss["Volumes"][l].ToString());
                }
            }
            catch (FileNotFoundException)
            {
                //do nothing, failed to load file...
            }
        }
    }
	
	private void _on_Search_text_changed(String _new_text)
	{
    	//if search text changed, update the receipt list to display only that matches any part of the title
		
		receiptList.Clear();

        for (var l = 0; l < list.Count; l++)
        {
            string _title = list[l]["Title"].ToString();
            _title = _title.ToLower();
            _new_text = _new_text.ToLower();
            if (_title.Contains(_new_text))
            {
                receiptList.AddItem(list[l]["Title"].ToString());
            }
			
			receiptList.SortItemsByText();
        }
    }
	
	private void _on_ReceiptList_item_selected(int _index)
	{
        //if a receipt is selected from the list, display it info
        var _title = receiptList.GetItemText(_index);
		initialTitle = _title; //store title higher in the scope as well to be used outside

        for (var l = 0; l < list.Count; l++) //add each item to list
        {
            if(_title == list[l]["Title"].ToString())
            {
                receiptIngredients.Clear();
				IngredientList.Clear();

                receiptTitle.Text = _title;
                receiptDescription.Text = list[l]["Description"].ToString();
                var _ingredientList = list[l]["Ingredients"].ToList();
                for (var i = 0; i < _ingredientList.Count; i++)
                {
                    var _amount = _ingredientList[i]["Amount"].ToString();
                    var _volume = _ingredientList[i]["Volume"].ToString();
                    var _ingredient = _ingredientList[i]["Ingredient"].ToString();
                    receiptIngredients.AddItem($"{_amount} {_volume} {_ingredient}");
					IngredientList.Add(new Ingredient(_amount, _volume, _ingredient));
                }
            }
        }
    }
	
	private void _on_ReceiptIngredients_item_rmb_selected(int _index, Vector2 _at_position)
	{
    	//if item rmb selected, display popup menu
		//if item is selected with right mouse button, display popup menu
		ingredientsPopup.Show();

        //set position of popup menu to the position of the item
        ingredientsPopup.SetGlobalPosition(GetViewport().GetMousePosition());
	}
	
	private void _on_IngredientsDeleteButton_pressed()
	{
    	//if ingredients popup delete button is pressed, delete selected item
        var _id = receiptIngredients.GetSelectedItems();
        receiptIngredients.RemoveItem(_id[0]);
        IngredientList.RemoveAt(_id[0]);	
	}
	
	private void _on_AddIngredientButton_button_up()
	{
        //if add ingredient button pressed, add the ingredient to the list but only if a receipt is selected
        if (receiptList.GetSelectedItems().Length > 0)
        {
			receiptSearch.Text = "";
			ingredientSearchList.Clear();
			
            var _amount = ingredientAmountOption.GetText();
            var _volume = ingredientVolumeOption.GetText();
            var _ingredient = ingredientOption.GetText();
			
			bool _match = false; //check if ingredient with the same name already exist
			for(var i = 0; i < IngredientList.Count; i++)
			{
				if(IngredientList[i].Ingredients == _ingredient)
					_match = true;
			}
			
            if (_match == false && _amount != "0" && _amount != "" && _amount != " ")
            {
                receiptIngredients.AddItem($"{_amount} {_volume} {_ingredient}");
                IngredientList.Add(new Ingredient(_amount, _volume, _ingredient));
            }
			else
			{
				//if ingredient with same name already exist, display  amessage
				_displayMessage(ingredientAlreadyExistMsg);
				
			}
        }
	}
	
	private void _on_UpdateButton_pressed()
	{
    	//if update button is pressed and we have a receipt selected, update the selected receipt		
        //check if any receipt selected or not
        if(receiptList.GetSelectedItems().Length > 0)
        {
			
			//if the title of the receipt has been changed, check if there is any receipt with the same title already exist in JSON
        	bool _alreadyExist = false;
			if(initialTitle.ToLower() != receiptTitle.Text.ToLower())
			{
				
				for(var t = 0; t < list.Count; t++)
	            {
	                if(receiptTitle.Text == list[t]["Title"].ToString())
	                {
	                    //display message receipt with the same title already exist
	                    _displayMessage(receiptAlreadyExistMsg);
						_alreadyExist = true;
	                }
	            }
			}

            //if receipt does not exist already with the same title, update the receipt
            if (!_alreadyExist)
            {
                if (receiptList.GetSelectedItems().Length > 0) //make sure item is selected
                {
                    var _selectedReceipt = receiptList.GetSelectedItems();

                    //find the receipt in the JSON data (list) that we have selected
                    for (var r = 0; r < list.Count; r++)
                    {
                        if (list[r]["Title"].ToString() == receiptList.GetItemText(_selectedReceipt[0]))
                        {

                            //update title in list
                            list[r]["Title"] = receiptTitle.Text;

                            //update description in list
                            list[r]["Description"] = receiptDescription.Text;

                            //update ingredients in the list
                            //build a new list of ingredients
                            StringBuilder _sb = new StringBuilder();
                            StringWriter _sw = new StringWriter(_sb);

                            using (JsonWriter _writer = new JsonTextWriter(_sw))
                            {
                                _writer.Formatting = Formatting.Indented;

                                _writer.WriteStartArray();
                                for (var i = 0; i < receiptIngredients.GetItemCount(); i++)
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

                                //add new list of ingredients to the list
                                list[r]["Ingredients"] = JArray.Parse(_sw.ToString());

                                //rebuild the entire JSON from scratch
                                //JArray _o = (JArray)JToken.FromObject(list); //converting the list to JArray, could not find a simple way to convert JArray or list to JSON string
                                //so we build the entire JSON from scratch line by line instead :P

                                //clear json writerss and builders first
                                _writer.Flush();
                                _sw.Flush();
                                _sb.Clear();

                                //reference the json file we are about to write in to
                                StreamWriter _w = new StreamWriter(receiptPath);

                                //build new json
                                _writer.Formatting = Formatting.Indented;

                                _writer.WriteStartObject();
                                _writer.WritePropertyName("Receipts");
                                _writer.WriteStartArray();
                                for (var nj = 0; nj < list.Count(); nj++)
                                {
                                    _writer.WriteStartObject();
                                    _writer.WritePropertyName("Title");
                                    _writer.WriteValue(list[nj]["Title"]);
                                    _writer.WritePropertyName("Description");
                                    _writer.WriteValue(list[nj]["Description"]);
                                    _writer.WritePropertyName("Ingredients");
                                    _writer.WriteStartArray();
                                    for (var i = 0; i < list[nj]["Ingredients"].Count(); i++)
                                    {
                                        _writer.WriteStartObject();
                                        _writer.WritePropertyName("Amount");
                                        _writer.WriteValue(list[nj]["Ingredients"][i]["Amount"]);
                                        _writer.WritePropertyName("Volume");
                                        _writer.WriteValue(list[nj]["Ingredients"][i]["Volume"]);
                                        _writer.WritePropertyName("Ingredient");
                                        _writer.WriteValue(list[nj]["Ingredients"][i]["Ingredient"]);
                                        _writer.WriteEndObject();
                                    }
                                    _writer.WriteEndArray();
                                    _writer.WriteEndObject();
                                }

                                _writer.WriteEndArray();
                                _writer.WriteEndObject();

                                //write new json in to file
                                _w.Write(_sw);
                                _w.Close();


                            }


                            //update the receipt lists to display if title has changed
                            receiptList.Clear();
                            for (var l = 0; l < list.Count; l++) //add each item to list
                            {
                                receiptList.AddItem(list[l]["Title"].ToString());
                            }

                            //sort list alphabetically
                            receiptList.SortItemsByText();

                            //show dialog, save was successfull.
                            _displayMessage(receiptUpdatedMsg);
							
							//break from the for loop once we have the receipt updated
							break;

                        }
						
                    }
                }

                //once we done updating the receipt and the list displaying the receipts, it is deselect the receipt in the list.
                //we have two choice, reset the form or select the receipt again

                //select receipt again
                for(var r = 0; r < receiptList.GetItemCount(); r++)
                {
                    if(receiptList.GetItemText(r) == receiptTitle.Text)
                    {
                        receiptList.Select(r);
						initialTitle = receiptTitle.Text;
                    }
                }
            }
            else //if receipt exist already with the same title, ask the user to enter a different title.
            {
                _displayMessage(receiptAlreadyExistMsg);
            }

        }
        else //if not receipt is selected just reset the form
        {
            _resetForm();
        }
		
		
    }
	
	private void _on_PrintButton_pressed()
	{
    	//if print button is pressed and a receipt is selected, send selected receipt to printer
		//TODO: need to fix letter spacing when printing on paper
		var _selectedReceipt = receiptList.GetSelectedItems();
		
		if(receiptList.GetSelectedItems().Length > 0)
		{
            //write receipt in to file first
            var _path = "data/print.txt";
			
        	StreamWriter _w = new StreamWriter(_path);

            _w.WriteLine(receiptTitle.Text);
            _w.WriteLine("");
            _w.WriteLine(receiptDescription.Text);
            _w.WriteLine("");
            _w.WriteLine("Ingredients:");
            for (var i = 0; i < receiptIngredients.GetItemCount(); i++)
            {
                _w.WriteLine(receiptIngredients.GetItemText(i));
            }
            _w.Close();

            //print file using System.Drawing.Printing
            StreamReader _r = new StreamReader(_path);
            string s = _r.ReadToEnd();

            PrintDocument p = new PrintDocument();
            p.PrintPage += delegate (object sender1, PrintPageEventArgs e1)
            {
                e1.Graphics.DrawString(s, new System.Drawing.Font("Times New Roman", 12), new System.Drawing.SolidBrush(System.Drawing.Color.Black), new System.Drawing.RectangleF(0, 0, p.DefaultPageSettings.PrintableArea.Width, p.DefaultPageSettings.PrintableArea.Height));

            };
            try
            {
                p.Print();
            }
            catch (Exception ex)
            {
                throw new Exception("Unable to print...", ex);
            }

            _r.Close();

        }
    }
	
	private void _on_SaveButton_pressed()
	{
        //if save button is pressed and a receipt is selected, display directory select dialog
        var _selectedReceipt = receiptList.GetSelectedItems();
		
		if(receiptList.GetSelectedItems().Length > 0)
		{
            //display dialog
            saveFileDialog.Popup_();
			
		}
	}

	private void _on_FileDialog_dir_selected(String _dir)
	{
        //if directory is selected to save the receipt, save it to file

        //get directory and file name the user entered and the selected receipt from the list
        var _filename = saveFileDialog.GetCurrentFile();
        var _directory = _dir;
        var _selectedReceipt = receiptList.GetSelectedItems();

        //if file name is empty, set file name to be the receipt name
        if (_filename == "" || _filename == " ")
        {
            _filename = receiptList.GetItemText(_selectedReceipt[0]) + ".txt";
        }

        //if file name does not include ".txt" extension, add it
        if(!_filename.Contains(".txt"))
        {
            _filename += ".txt";
        }

        //create path to file we are about to write
        string _path = $"{_directory}/{_filename}";

        //write receipt in to file
        StreamWriter _w = new StreamWriter(_path);

        _w.WriteLine(receiptTitle.Text);
        _w.WriteLine("");
        _w.WriteLine(receiptDescription.Text);
        _w.WriteLine("");
        _w.WriteLine("Ingredients:");
        for(var i = 0; i < receiptIngredients.GetItemCount(); i++)
        {
            _w.WriteLine(receiptIngredients.GetItemText(i));
        }
        _w.Close();
		
		//show dialog, save was successfull.
		_displayMessage(receiptSavedToFileMsg);


    }
	
	private void _on_IngredientSearchAddButton_pressed()
	{
        //if ingredient search add button is pressed, add ingredient to list only if not already in the list
        bool _match = false;
        for(var i = 0; i < ingredientSearchList.GetItemCount(); i++)
        {
            if (ingredientSearchList.GetItemText(i) == ingredientSearchOption.Text)
                _match = true;
        }

        if(_match != true) 
        {
            ingredientSearchList.AddItem(ingredientSearchOption.Text);
        }


    }
	
	private void _on_IngredientSearchList_item_rmb_selected(int _index, Vector2 _at_position)
	{
    	//if item right mouse button selected, display delete button
		ingredientSearchPopup.Show();

        //set position of popup menu to the position of the item
        ingredientSearchPopup.SetGlobalPosition(GetViewport().GetMousePosition());
	}
	
	private void _on_IngredientsSearchDeleteButton_pressed()
	{
    	//if the delete button is pressed in the ingredient search list, delete selected item
		var _id = ingredientSearchList.GetSelectedItems();
        ingredientSearchList.RemoveItem(_id[0]);	
		
	}

    private void _resetForm() //method to reset the form
    {
        initialTitle = "";
        receiptTitle.Text = "";
        receiptDescription.Text = "";
        receiptIngredients.Clear();
        ingredientAmountOption.Text = "";
    }
	
	private void _displayMessage(string _msg) //method to display a popup message
	{
		Label _popupMessage = (Label)confirmationPopupDialog.GetNode("MessageLabel");
		_popupMessage.Text = _msg;
        confirmationPopupDialog.Show();
	}

}
