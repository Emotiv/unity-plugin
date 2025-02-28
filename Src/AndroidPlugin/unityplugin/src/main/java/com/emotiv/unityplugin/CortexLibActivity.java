package com.emotiv.unityplugin;

import android.app.Application;
import android.Manifest;
import android.content.pm.PackageManager;
import android.os.Build;
import android.os.Bundle;
import android.util.Log;

import androidx.annotation.NonNull;
import androidx.appcompat.app.AppCompatActivity;
import androidx.core.app.ActivityCompat;
import androidx.core.content.ContextCompat;
import org.json.JSONObject;

import com.emotiv.CortexLibInterface;
import com.emotiv.CortexLogLevel;
import com.emotiv.CortexLogHandler;
import android.util.Log;
/**
 * This class is the base class for MainActivity when they want to work with EmotivCortexLib.aar
 * In this activity, we will request some permissions needed by CortexLib and start/stop CortexLib.
 */
public class CortexLibActivity implements CortexLibInterface {
    private final String TAG = CortexLibActivity.class.getName();
    private boolean mCortexStarted = false;
    protected CortexConnection mCortexConnection = null;
    protected CortexConnectionInterface mCortexConnectionItf = null;

    protected JavaLogInterface mJavaLogInterface = null;

    private static final CortexLibActivity ourInstance = new CortexLibActivity();

    public static CortexLibActivity getInstance() {
        return ourInstance;
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
            CortexLibManager.setLogHandler(CortexLogLevel.INFO, s -> {
                if(mJavaLogInterface != null) {
                    mJavaLogInterface.onReceivedLog(s);
                }
            });
            CortexLibManager.start(this);
        }
    }

    public void setJavaLogInterface(JavaLogInterface javaLogInterface) {
        mJavaLogInterface = javaLogInterface;
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
