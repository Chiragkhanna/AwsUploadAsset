# AwsUploadAsset
With the help of this solution we can migrate our assets like images, audio and video from on-premise file system to AWS S3 bucket
The utility provides you provide multiple options like
1) Sync Assets
2) Delete Assets
3) List Assets
4) LifeCycle Configuration ( To set the expiry time of assets present in S3 and after expiry move them to vault)
5) Exit

In Sync Assets, it will ask for the the path from where you need to copy the asset to AWS
and then it will start placing the images/audio to S3 bucket

In order to delete some asset we can either give the key name (the name assign to every uploaded file)
or the relative path for deletion
