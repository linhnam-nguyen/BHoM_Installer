<xsl:stylesheet version="1.0"
            xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
            xmlns:wix="http://wixtoolset.org/schemas/v4/wxs"
            xmlns="http://wixtoolset.org/schemas/v4/wxs"
            exclude-result-prefixes="wix">

  <xsl:output method="xml" indent="yes" omit-xml-declaration="yes" />

  <xsl:strip-space elements="*"/>

  <xsl:template match="@*|node()">
    <xsl:copy>
      <xsl:apply-templates select="@*|node()"/>
    </xsl:copy>
  </xsl:template>

  <!-- Suppress Class and TypeLib elements which cause ICE errors or WIX0047 when not parented -->
  <xsl:template match="wix:Class" />
  <xsl:template match="wix:TypeLib" />

</xsl:stylesheet>
