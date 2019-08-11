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

public class Options : Node
{
    Button ingredientAddButton;
    ItemList ingredientItemList;
    LineEdit ingredientLineEdit;
	Button volumeAddButton;
    ItemList volumeItemList;
    LineEdit volumeLineEdit;
	Label ingredientLabel;
	Label volumeLabel;
	Label languageLabel;
	OptionButton languageOption;
	Label dataPathLabel;
	LineEdit dataPathLineEdit;
	Button dataPathSelectButton;
	FileDialog fileDialog;
	
	StreamReader r;
	string json;
	JObject rss;
	
	string ingredientFile;
	string volumeFile;
	string receiptFile;
	
	string storagePath;

    //public values to access data files from anywhere
    public static string IngredientPath {get; set;}
	public static string VolumePath {get; set;}
	public static string ReceiptPath {get; set;}
    public static string ConfigurationPath { get; set; }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
		//get reference to files and paths
		storagePath = "data/"; //data folder in app directory is the default location to store data
		ingredientFile = "ingredient-list.json";
		volumeFile = "volume-list.json";
		receiptFile = "receipt-list.json";
		ConfigurationPath = "data/configuration.json";
        IngredientPath = $"{storagePath}{ingredientFile}";
        VolumePath = $"{storagePath}{volumeFile}";
        ReceiptPath = $"{storagePath}{receiptFile}";

        //get reference to the controls.
        ingredientAddButton = (Button)GetNode("IngredientsAddButton");
        ingredientItemList = (ItemList)GetNode("IngredientsItemList");
        ingredientLineEdit = (LineEdit)GetNode("IngredientsLineEdit");
		volumeAddButton = (Button)GetNode("VolumeAddButton");
        volumeItemList = (ItemList)GetNode("VolumeItemList");
        volumeLineEdit = (LineEdit)GetNode("VolumeLineEdit");
		ingredientLabel = (Label)GetNode("IngredientsLabel");
		volumeLabel = (Label)GetNode("VolumeLabel");
		languageLabel = (Label)GetNode("LanguageLabel");
		languageOption = (OptionButton)GetNode("LanguageOption");
		dataPathLabel = (Label)GetNode("DataPathLabel");
		dataPathLineEdit = (LineEdit)GetNode("DataPathLineEdit");
		dataPathSelectButton = (Button)GetNode("DataPathSelectButton");
		fileDialog = (FileDialog)GetNode("FileDialog");
		
		//get language and storage configuration from JSON
		r = new StreamReader(ConfigurationPath);
		json = r.ReadToEnd();
		r.Close();
		
		rss = JObject.Parse(json); //convert string to object
        string _language = rss["Language"].ToString();
        if(rss["StoragePath"].ToString() != "data/")
        {
            storagePath = rss["StoragePath"].ToString();
			
			//update ingredient and volume path
			IngredientPath = $"{storagePath}/{ingredientFile}";
			VolumePath = $"{storagePath}/{volumeFile}";
			ReceiptPath = $"{storagePath}/{receiptFile}";
        }

        dataPathLineEdit.Text = storagePath; //display path in the edit box

        //now get the language file
        r = new StreamReader("data/languages/lang.json");
		json = r.ReadToEnd();
		r.Close();
		
		rss = JObject.Parse(json);
		string _langFile = rss[_language].ToString();

        //add each language from lang.json to options
        foreach (JProperty property in rss.Properties())
        {
            languageOption.AddItem(property.Name);
        }
        //set language option to display current language
        for(var l = 0; l < languageOption.GetItemCount(); l++)
        {
            if(languageOption.GetItemText(l) == _language)
            {
                languageOption.Select(l);
            }
        }


        //now get all the text for the selected language
        r = new StreamReader($"data/languages/{_langFile}.json");
		json = r.ReadToEnd();
		r.Close();	
		rss = JObject.Parse(json);
		
		//now change text on all buttons and labels
		this.SetName(rss["Options"].ToString());
		ingredientLabel.Text = rss["Ingredients"].ToString();
		volumeLabel.Text = rss["Volumes"].ToString();
		ingredientAddButton.Text = rss["Add"].ToString();
		volumeAddButton.Text = rss["Add"].ToString();
		languageLabel.Text = rss["Language"].ToString();
        dataPathLabel.Text = rss["Location to store data"].ToString();
        dataPathSelectButton.Text = rss["Select"].ToString();
        fileDialog.SetTitle(rss["Select folder"].ToString());

		 
        //read list of ingredient items from JSON and add it to the item list
        try
        {

            r = new StreamReader(IngredientPath);
            json = r.ReadToEnd();
            r.Close();

            rss = JObject.Parse(json); //convert string to object
            var _list = rss["Ingredients"].ToList(); //convert to list using Linq
            for (var l = 0; l < _list.Count; l++) //add each item to list
            {
                ingredientItemList.AddItem(rss["Ingredients"][l].ToString());
            }
        }
        catch(FileNotFoundException)
        {
            //do nothing, load of file is failed
        }


        //read list of volume items from JSON and add it to the item list
		try
		{
            r = new StreamReader(VolumePath);
            json = r.ReadToEnd();
	        r.Close();
	
	        rss = JObject.Parse(json); //convert string to object
	        var _list = rss["Volumes"].ToList(); //convert to list using Linq
	        for(var l = 0; l < _list.Count; l++) //add each item to list
	        {
	            volumeItemList.AddItem(rss["Volumes"][l].ToString());
	        }
		}
        catch(FileNotFoundException)
        { 
            //do nothing, load of file is failed
        }


    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
   // public override void _Process(float delta)
    //{
   // }


    private void _on_AddButton_button_up()
    {
		//get text from edit box
		string _item = ingredientLineEdit.Text;
		
		//remove any white spaces from the beginning of the text
		if(_item.Length > 0)
		{
			while(_item[0] == ' ')
	        {
	            _item = _item.Remove(0, 1);
	        }
		}
		
		//check if the item already exist in the list, if it does we break
        if (_item != "" && ingredientItemList.GetItemCount() > 0)
        {
            
            for(var i = 0; i < ingredientItemList.GetItemCount(); i++)
            {
                if(ingredientItemList.GetItemText(i) == _item)
                {
					ingredientLineEdit.Text = "";
                    _item = "";
					break;
                }
            }
        }
		
		//if at this point item is still not an empty string that means it is valid and add it to the list
        if(_item != "" && _item != " ")
        {
            
			//add item to list
            ingredientItemList.AddItem(_item);
			//sort list alphabetically
            ingredientItemList.SortItemsByText();
			
            //write items in to JSON
            writeIngredientItemsToJSON();

            //delete text from text entry
            ingredientLineEdit.Text = "";
        }
    }
	
	private void _on_VolumeAddButton_button_up()
	{
	    //get text from edit box
		string _item = volumeLineEdit.Text;
		
		//remove any white spaces from the beginning of the text
		if(_item.Length > 0)
		{
			while(_item[0] == ' ')
	        {
	            _item = _item.Remove(0, 1);
	        }
		}
		
		//check if the item already exist in the list, if it does we break
        if (_item != "" && volumeItemList.GetItemCount() > 0)
        {
            
            for(var i = 0; i < volumeItemList.GetItemCount(); i++)
            {
                if(volumeItemList.GetItemText(i) == _item)
                {
					volumeLineEdit.Text = "";
                    _item = "";
					break;
                }
            }
        }
		
		//if at this point item is still not an empty string that means it is valid and add it to the list
        if(_item != "" && _item != " ")
        {
            
			//add item to list
            volumeItemList.AddItem(_item);
			//sort list alphabetically
            volumeItemList.SortItemsByText();
			
            //write items in to JSON
            writeVolumeItemsToJSON();

            //delete text from text entry
            volumeLineEdit.Text = "";
        }
	}
	
	private void _on_LanguageOption_item_selected(int _id)
	{
        //if different language is selected, update configuration
        updateConfiguration();

        //reload the scene to apply language
        GetTree().ReloadCurrentScene();
	}
	
	private void _on_DataPathSelectButton_pressed()
	{
    	//when select button is clicked, show the file dialog window
		fileDialog.Show();
	}
	
	private void _on_FileDialog_dir_selected(String _dir)
	{
        //if directory is selected, update storage path.
        storagePath = _dir;
        dataPathLineEdit.Text = _dir;
		
		//update ingredient and volume path
		IngredientPath = $"{storagePath}/{ingredientFile}";
		VolumePath = $"{storagePath}/{volumeFile}";
		ReceiptPath = $"{storagePath}/{receiptFile}";
		
		//update configuration
		updateConfiguration();
		
		//check if any ingredient, volume and receipt list file exist and copy them over to the new location
		var _file = new Godot.File();
        var _directory = new Godot.Directory();
        if(_file.FileExists($"data/{volumeFile}"))
        {
            _directory.Copy($"data/{volumeFile}",VolumePath);
        }
		if(_file.FileExists($"data/{ingredientFile}"))
        {
            _directory.Copy($"data/{ingredientFile}",IngredientPath);
        }
		if(_file.FileExists($"data/{receiptFile}"))
        {
            _directory.Copy($"data/{receiptFile}",ReceiptPath);
        }


    }
	
	private void updateConfiguration()
	{
		//if different language is selected, write selected language to JSON
        StringBuilder _sb = new StringBuilder();
        StringWriter _sw = new StringWriter(_sb);
        StreamWriter _w = new StreamWriter(ConfigurationPath);

        using (JsonWriter _writer = new JsonTextWriter(_sw))
        {
            _writer.Formatting = Formatting.Indented;

            _writer.WriteStartObject();
            _writer.WritePropertyName("Language");
            _writer.WriteValue(languageOption.GetItemText(languageOption.GetSelected()));
			_writer.WritePropertyName("StoragePath");
            _writer.WriteValue(storagePath);
            _writer.WriteEndObject();
            _w.Write(_sw);
            _w.Close();
        }
	}
	
	private void writeVolumeItemsToJSON()
	{
		StringBuilder _sb = new StringBuilder();
        StringWriter _sw = new StringWriter(_sb);
        StreamWriter _w = new StreamWriter(VolumePath);

        using (JsonWriter _writer = new JsonTextWriter(_sw))
        {
            _writer.Formatting = Formatting.Indented;

            _writer.WriteStartObject();
            _writer.WritePropertyName("Volumes");
            _writer.WriteStartArray();
			for(var i = 0; i < volumeItemList.GetItemCount(); i++)
            {
                _writer.WriteValue(volumeItemList.GetItemText(i));
            }
            _writer.WriteEnd();
            _writer.WriteEndObject();
            _w.Write(_sw);
            _w.Close();
        }
	}
	
	private void writeIngredientItemsToJSON()
	{
		StringBuilder _sb = new StringBuilder();
        StringWriter _sw = new StringWriter(_sb);
        StreamWriter _w = new StreamWriter(IngredientPath);

        using (JsonWriter _writer = new JsonTextWriter(_sw))
        {
            _writer.Formatting = Formatting.Indented;

            _writer.WriteStartObject();
            _writer.WritePropertyName("Ingredients");
            _writer.WriteStartArray();
			for(var i = 0; i < ingredientItemList.GetItemCount(); i++)
            {
                _writer.WriteValue(ingredientItemList.GetItemText(i));
            }
            _writer.WriteEnd();
            _writer.WriteEndObject();
            _w.Write(_sw);
            _w.Close();
        }
	}
}

