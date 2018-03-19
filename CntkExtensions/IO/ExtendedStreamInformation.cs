using CNTK;

namespace CntkExtensions.IO
{
    public class ExtendedStreamInformation : StreamInformation
    {
        public ExtendedStreamInformation(string name, uint id, StorageFormat storageFormat,
            DataType elementType, NDShape sampleShape, bool isBinary)
        {
            m_name = name;
            m_id = id;
            m_storageFormat = storageFormat;
            m_elementType = elementType;
            m_sampleLayout = sampleShape;
            m_isBinary = isBinary;
        }
    }
}
