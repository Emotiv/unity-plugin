package com.emotiv.unityplugin;

import android.os.Bundle;
import com.unity3d.player.UnityPlayerActivity;
import android.os.Build;
import android.content.Intent;
import android.content.pm.ApplicationInfo;
import android.content.pm.PackageManager;
import android.util.Log;

public class CustomUnityPlayerActivity extends UnityPlayerActivity {

    private static final String TAG = "CustomUnityPlayerActivity";
    @Override
    protected void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);
        Log.i(TAG, "onCreate: CustomUnityPlayerActivity is created");
        // Additional initialization if needed
        startBForegroundService();
    }

    @Override
    protected void onPause() {
        super.onPause();
    }

    @Override
    protected void onResume() {
        super.onResume();
        Log.i(TAG, "onResume: CustomUnityPlayerActivity is resumed");
    }

    public void startBForegroundService() {
        Intent startIntent = new Intent(this, BForegroundService.class);
        startIntent.setAction("Start");
        // get app name
        String appName = getApplicationName();
        startIntent.putExtra("inputExtra", appName + " is running.");
        startIntent.putExtra("appName", appName);
        runService(startIntent);
    }

    public void stopBForegroundService() {
        Intent stopIntent = new Intent(this, BForegroundService.class);
        stopIntent.setAction("Stop");
        runService(stopIntent);
    }

    private void runService(Intent intent) {
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O)
            startForegroundService(intent);
        else
            startService(intent);
    }

    @Override
    protected void onDestroy() {
        super.onDestroy();
        // Handle activity destroy
        stopBForegroundService();
    }

    private String getApplicationName() {
        ApplicationInfo applicationInfo = getApplicationInfo();
        PackageManager packageManager = getPackageManager();
        return (String) packageManager.getApplicationLabel(applicationInfo);
    }
}
