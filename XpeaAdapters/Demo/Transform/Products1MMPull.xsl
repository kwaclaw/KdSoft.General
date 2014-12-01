<?xml version='1.0' encoding='utf-8' ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"><xsl:output method="html"/>
	
	<xsl:include href="Style1.xsl"/>
	<xsl:template match="/">
		<html>
			<body>
				<!-- Set Formatting Characteristics -->
				<xsl:call-template name="Style1"/>

				<head>
					<title>Stylesheet Example</title>
				</head>
				<h1>Product Inventory Information for different regions</h1>

			</body>
		</html>

	<xsl:for-each select="inventory">
		<span class="subhead">Inventory Listing</span>
		<BR/><br/>
		
		<xsl:for-each select="region">
			<span class="subhead">Region</span>
			<span class="text">

			<xsl:value-of select="@area" />
			</span>
			<br/>
			<br/>
			<table border="1">
				<tr>
					<th>ID</th>
					<th>Name</th>
					<th>Price</th>
					<th>Quantity</th>
				</tr>
				<xsl:for-each select="product">
					<xsl:sort select="name" order="ascending"/>
					<tr>
						<td>
							<xsl:value-of select="prodid"/>
						</td>
						<td>
							<xsl:value-of select="name"/>
						</td>
						<td>
							<xsl:value-of select="price"/>
						</td>
						<td>
							<xsl:value-of select="quantity"/>
						</td>
					</tr>
				</xsl:for-each>
			</table>
		Total Products in region: <xsl:value-of select="count(product)"/><br/>
		Total Inventory in region: <xsl:value-of select="sum(product/quantity)"/><br/>

		
		<br/>
		</xsl:for-each>
</xsl:for-each>
	</xsl:template>
</xsl:stylesheet>
