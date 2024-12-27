package com.emotiv.unityplugin;
import android.content.Intent;
import android.os.Bundle;
import com.unity3d.player.UnityPlayerActivity;
import android.util.Log;

public class CustomUnityPlayerActivity extends UnityPlayerActivity {
    private static final String TAG = "CustomUnityPlayerActivity";

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        // Additional initialization if needed
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        Log.d(TAG, "onActivityResult: requestCode: " + requestCode + ", resultCode: " + resultCode);
        // Handle the result here
        if (requestCode == 100) {
            // Process the result
            // Log.d(TAG, "requestCode: " + requestCode + ", resultCode: " + resultCode);


        }
    }

    // public String getAuthenticationCode(int requestCode, Intent intent) {
    //     String result = "";
    //     if (requestCode != this.handeCode) {
    //         Log.e("[EmotivCortexLib error]:", "requestCode is not correct " + requestCode);
    //         return result;
    //     } else {
    //         AuthorizationException authException = AuthorizationException.fromIntent(intent);
    //         if (authException != null) {
    //             Log.e("[EmotivCortexLib error]:", "authentication error " + authException.errorDescription);
    //             return result;
    //         } else {
    //             AuthorizationResponse authResponse = AuthorizationResponse.fromIntent(intent);
    //             if (authResponse == null) {
    //                 Log.e("[EmotivCortexLib error]:", "can not get authentication response");
    //             } else {
    //                 result = authResponse.authorizationCode;
    //             }

    //             return result;
    //         }
    //     }
    // }
}