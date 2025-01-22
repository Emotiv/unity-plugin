package com.emotiv.unityplugin;

import android.app.Application;
import android.Manifest;
import android.content.pm.PackageManager;
import android.os.Build;
import android.os.Bundle;
import android.util.Log;
import android.content.Intent;

import androidx.annotation.NonNull;
import androidx.appcompat.app.AppCompatActivity;
import androidx.core.app.ActivityCompat;
import androidx.core.content.ContextCompat;
import org.json.JSONObject;

import com.emotiv.CortexLibInterface;
import android.util.Log;

import com.unity3d.player.UnityPlayerActivity;

public class CustomUnityPlayerActivity extends UnityPlayerActivity implements CortexLibInterface {
    private static final String TAG = "CustomUnityPlayerActivity";
    private boolean mCortexStarted = false;
    protected CortexConnection mCortexConnection = null;
    protected CortexConnectionInterface mCortexConnectionItf = null;
    

    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        Log.i(TAG, "CustomUnityPlayerActivity is created");
    }

    @Override
    protected void onDestroy() {
        super.onDestroy();
        Log.i(TAG, "CustomUnityPlayerActivity is destroyed");
    }

    @Override
    protected void onActivityResult(int requestCode, int resultCode, Intent data) {
        super.onActivityResult(requestCode, resultCode, data);
        Log.i(TAG, "onActivityResult: requestCode: " + requestCode + ", resultCode: " + resultCode);
        if (mCortexConnection != null) {
            String authorizationCode = mCortexConnection.getAuthenticationCode(requestCode, data);
            // print authorizationCode to Unity console
            Log.i(TAG, "Authorization code: " + authorizationCode);
        }
    }

    public void authenticate(String clientId, int requestCode) {
        // PRINT LOG
        Log.i(TAG, "authenticate: clientId: " + clientId + ", requestCode: " + requestCode);
        if (mCortexConnection != null) {
            mCortexConnection.authenticate(this, clientId, requestCode);
        }
    }

    public void load(Application application) {
        CortexLibManager.load(application);
    }

    public void start(CortexConnectionInterface cortexResponseInterface) {
        if (mCortexStarted) {
            onCortexStarted();
        }
        else {
            mCortexConnectionItf = cortexResponseInterface;
            CortexLibManager.start(this);
        }
    }

    public void stop() {
        CortexLibManager.stop();
    }

    public void sendRequest(String contentRequest) {
        if (mCortexConnection != null) {
            mCortexConnection.sendRequest(contentRequest);
        }
    }

    @Override
    public void onCortexStarted() {
        mCortexStarted = true;
        mCortexConnection = CortexLibManager.createConnection(mCortexConnectionItf);
        mCortexConnectionItf.onCortexStarted();
    }
}