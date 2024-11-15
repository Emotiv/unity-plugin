package com.emotiv.unityplugin;

import android.app.Activity;
import android.content.Intent;
import android.os.StrictMode;
import android.util.Log;

import androidx.annotation.NonNull;

import com.emotiv.CortexClient;
import com.emotiv.ResponseHandler;
import org.json.JSONObject;

public class CortexConnection implements ResponseHandler {
    private final String TAG = CortexConnection.class.getName();
    private CortexConnectionInterface mCortexConnectionInterface;
    // private int mCurrentStreamID = -1;
    private CortexClient mCortexClient = null;

    public CortexConnection() {
        // TODO: What is thread policy
        StrictMode.ThreadPolicy policy = new StrictMode.ThreadPolicy.Builder().permitAll().build();
        StrictMode.setThreadPolicy(policy);
    }

    public void setCortexLibConnectionInterface(CortexConnectionInterface cortexConnectionInterface) {
        this.mCortexConnectionInterface = cortexConnectionInterface;
    }

    public void open() {
        mCortexClient = new CortexClient();
        mCortexClient.registerResponseHandler(this);
    }

    public void close() {
        mCortexClient.close();
    }

    public void sendRequest(String contentRequest) {
        try {
            if (mCortexClient != null) {
                
                mCortexClient.sendRequest(contentRequest);
                Log.i(TAG, contentRequest);
            }
        } catch (Exception e) { e.printStackTrace(); }
    }

    public void authenticate(@NonNull Activity activity, String clientId, int activityHandleCode)
    {
        try {
            if (mCortexClient != null) {
                mCortexClient.authenticate(activity, clientId, activityHandleCode);
            }
        } catch (Exception e) { e.printStackTrace(); }

    }

    public String getAuthenticationCode(int requestCode, @NonNull Intent intent)
    {
        String result = "";
        try {
            if (mCortexClient != null) {
                result = mCortexClient.getAuthenticationCode(requestCode, intent);
            }
        } catch (Exception e) { e.printStackTrace(); }
        return result;
    }

    @Override
    public void processResponse(String s) {
        try {
            mCortexConnectionInterface.onReceivedMessage(s);

            // JSONObject jsonObj = new JSONObject(s);
            // // received warning message in response to a RPC request
            // if (jsonObj.has("warning")) {
            //     mCortexConnectionInterface.onReceivedWarningMessage(s);
            // }
            // // error
            // else if (jsonObj.has("error")) {
            //     JSONObject error = jsonObj.getJSONObject("error");
            //     mCortexConnectionInterface.onError(error);
            // }
            // // received data from a data stream
            // else if (jsonObj.has("sid")) {
            //     mCortexConnectionInterface.onReceivedNewData(s);
            // }
            // // received message in response to a RPC request
            // else if (jsonObj.has("id")) {
            //     mCortexConnectionInterface.onReceivedMessage(s, mCurrentStreamID);
            // }

        } catch (Exception e) { e.printStackTrace(); }
    }
}