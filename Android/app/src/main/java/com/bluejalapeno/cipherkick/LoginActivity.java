package com.bluejalapeno.cipherkick;

import android.content.Intent;
import android.support.v7.app.AppCompatActivity;
import android.os.Bundle;
import android.view.View;
import android.widget.Button;

public class LoginActivity extends AppCompatActivity {

    @Override
    protected void onCreate(Bundle savedInstanceState) {

        // Call superclass onCreate
        super.onCreate(savedInstanceState);

        // Set our content view
        setContentView(R.layout.login);

        // Add a button click handler
        final Button loginButton = (Button) findViewById(R.id.login_button);
        loginButton.setOnClickListener(new View.OnClickListener() {
            public void onClick(View v) {
                // Transition to the list activity
                startActivity(new Intent(getApplicationContext(), ListActivity.class));
            }
        });
    }
}
