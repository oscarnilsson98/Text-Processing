import { FormBuilder, FormGroup } from '@angular/forms';
import { Component, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { saveAs } from 'file-saver';

@Component({
  selector: 'app-home',
  templateUrl: './home.component.html',
  styleUrls: ['./home.component.css']
})

export class HomeComponent implements OnInit {

  //Defines a formGroup
  uploadForm: FormGroup;  

  //Initiate my constructor
  constructor(private formBuilder: FormBuilder, private httpClient: HttpClient) {
  }

  //Function which runs when the program starts where uploadForm gets initiated
  ngOnInit() {
    this.uploadForm = this.formBuilder.group({
      profile: ['']
    });
  }

  //Function which runs when a file is selected to be uploaded
  //Puts the file in the uploadForm
  onFileSelect(event) {
    if (event.target.files.length > 0) {
      const file = event.target.files[0];
      this.uploadForm.get('profile').setValue(file);
    }
  }

  //Functions which runs when the user presses the "Process File" Button"
  //creates a constans formData which gets the uploaded file
  //sends the file to the api and gets a file back with the edited text => the magic happens behind
  onSubmit() {
    const formData = new FormData();

    formData.append('file', this.uploadForm.get('profile').value);

    this.httpClient.post("/textprocess", formData, { responseType: 'blob' }).subscribe(blob => {
      saveAs(blob, 'edited.txt', {
        type: 'text/plain;charset=windows-1252'
      });
    },
      error => {
        console.log(error)

        if (error.status == 404) {
          alert("Empty file")
        }

      }
    );
  }
}
