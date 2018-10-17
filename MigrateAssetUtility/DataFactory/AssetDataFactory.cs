
using Gartner.GlobalAssemblies.AdminBusiness;
using Gartner.GlobalAssemblies.AdminBusiness.Data;
using System.Collections.Generic;

namespace MigrateAssetUtility.DataFactory
{
    public class AssetDataFactory
    {
        AssetsManager _manager = new AssetsManager();
        public List<Assets> GetAppointmentAsset(int assetType, bool isMigration = false,int chunkSize = 500)
        {
            return _manager.GetAppointmentAsset(assetType, isMigration, chunkSize);
        }
        public int GetAppointmentAssetCount(int assetType, bool isMigration = false)
        {
            return _manager.GetAppointmentAssetCount(assetType, isMigration);
        }
        public bool UpdateAssetPath(long assetId, string cdnkey,string s3key)
        {
            return _manager.UpdateAssetPath(assetId, cdnkey,s3key  );
        }
    }
}
