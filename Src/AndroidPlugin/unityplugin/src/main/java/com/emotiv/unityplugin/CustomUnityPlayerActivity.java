package com.emotiv.unityplugin;
import android.content.Intent;
import android.os.Bundle;
import com.unity3d.player.UnityPlayer;
import com.unity3d.player.UnityPlayerActivity;
import net.openid.appauth.AuthorizationException;
import net.openid.appauth.AuthorizationResponse;
import android.util.Log;

public class CustomUnityPlayerActivity extends UnityPlayerActivity {
    private static final String TAG = "CustomUnityPlayerActivity";

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        // print log
        Log.i(TAG, "qqqq onCreate: CustomUnityPlayerActivity is created");
        // Additional initialization if needed
    }

    // override resume
    @Override
    protected void onResume() {
        super.onResume();
        // print log
        Log.i(TAG, "qqqq onResume: CustomUnityPlayerActivity is resumed");
        // Additional resume logic if needed
    }

    // override pause
    @Override
    protected void onPause() {
        super.onPause();
        // print log
        Log.i(TAG, "qqqq onPause: CustomUnityPlayerActivity is paused");
        // Additional pause logic if needed
    }

    // override new intent
    @Override
    protected void onNewIntent(Intent intent) {
        super.onNewIntent(intent);
        // print log
        Log.i(TAG, "qqqq onNewIntent: CustomUnityPlayerActivity is new intent");
        // Additional new intent logic if needed
    }

    // override onActivityResult
    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        Log.i(TAG, "qqqq onActivityResult: requestCode: " + requestCode + ", resultCode: " + resultCode);
        // Handle the result here
        // get the authentication code
        if (resultCode == RESULT_OK) {
            Log.i(TAG, "qqqq onActivityResult: resultCode is OK");
            String authCode = getAuthenticationCode(requestCode, data);
            Log.i(TAG, "qqqq onActivityResult: authCode: " + authCode);
            // build an json object with key is authenticationCode and value is authCode and convert it to string
            String authCodeJson = "{\"authenticationCode\":\"" + authCode + "\"}";
            // send the authentication code to Unity
            UnityPlayer.UnitySendMessage("CortexLibManager", "OnReceivedMessage", authCodeJson);

        } else if (resultCode == RESULT_CANCELED) {
            Log.i(TAG, "qqqq onActivityResult: resultCode is CANCELED");
        } else {
            Log.i(TAG, "qqqq onActivityResult: resultCode is UNKNOWN");
        }
    }

    public String getAuthenticationCode(int requestCode, Intent intent) {
        String result = "";
        if (requestCode != 100) {
            Log.e("[EmotivCortexLib error]:", "requestCode is not correct " + requestCode);
            return result;
        } else {
            AuthorizationException authException = AuthorizationException.fromIntent(intent);
            if (authException != null) {
                Log.e("[EmotivCortexLib error]:", "authentication error " + authException.errorDescription);
                return result;
            } else {
                AuthorizationResponse authResponse = AuthorizationResponse.fromIntent(intent);
                if (authResponse == null) {
                    Log.e("[EmotivCortexLib error]:", "can not get authentication response");
                } else {
                    result = authResponse.authorizationCode;
                }

                return result;
            }
        }
    }
}
