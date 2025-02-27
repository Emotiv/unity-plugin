#import <Foundation/Foundation.h>
#import <EmotivCortexLib/CortexClient.h>

@interface CortexLibIosEmbeddedConnection : NSObject<CortexClientDelegate> {
@private
    CortexClient *cortexClient;
}

+ (id _Nonnull) shareInstance;
- (void)sendRequest:(NSString * _Nonnull)nsJsonString;

# pragma mark CortexClientDelegate
- (void) processResponse:(NSString * _Nonnull)responseMessage;
@end
